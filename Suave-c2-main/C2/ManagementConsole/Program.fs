open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open System.Threading

let baseUrl = "http://192.168.8.107:8000"  // Corrected base URL
let httpClient = new HttpClient()
let libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/library")

// Function to create a watcher for the log directory of a specific session
let createLogWatcher (sessionId: string) (sessionScriptDir: string) (debugScriptDir: string) =
    // Define the log directory path for the session
    let logDir = Path.Combine(Directory.GetCurrentDirectory(), $"../Suave Web Server/bin/Debug/net8.0/clients/{sessionId}")

    // Define the event handling function
    let onLogChange (sender: obj) (args: FileSystemEventArgs) =
        let dirPath = sender :?> FileSystemWatcher |> fun watcher -> watcher.Path
        try
            printfn "Log change detected in %s for session %s." dirPath sessionId
            let currentScriptPath = Path.Combine(sessionScriptDir, "currentscript.txt")
            let debugCurrentScriptPath = Path.Combine(debugScriptDir, "currentscript.txt")
            
            // Check and delete currentscript.txt in the session script directory
            if File.Exists(currentScriptPath) then
                File.Delete(currentScriptPath)
                printfn "Current script removed from session directory."

            // Check and delete currentscript.txt in the debug script directory
            if File.Exists(debugCurrentScriptPath) then
                File.Delete(debugCurrentScriptPath)
                printfn "Current script removed from debug directory."
        with
        | ex -> printfn "Error while handling log change for session %s in %s: %s" sessionId dirPath ex.Message

    // Helper function to configure and start a watcher
    let configureWatcher dirPath =
        let watcher = new FileSystemWatcher(dirPath, "log.txt") 
        watcher.NotifyFilter <- NotifyFilters.LastWrite
        watcher.Changed.Add(fun args -> onLogChange watcher args)
        watcher.EnableRaisingEvents <- true
        watcher

    // Create and start watchers for both session and debug script directories
    let sessionWatcher = configureWatcher(sessionScriptDir)
    let debugWatcher = configureWatcher(debugScriptDir)

    // Return the watchers so they can be managed outside this function if necessary
    (sessionWatcher, debugWatcher)

// Example usage: create watchers for a specific session
let sessionScriptDir = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/currentscript/Session1")
let debugScriptDir = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/bin/Debug/net8.0/scripts/currentscript/Session1")
let watchers = createLogWatcher "Session1" sessionScriptDir debugScriptDir




// Function to list all clients
let listClients () =
    async {
        try
            let! response = httpClient.GetAsync(sprintf "%s/clients" baseUrl) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "\nSessions:\n%s\n" content
            else
                printfn "Error: %s" response.ReasonPhrase
        with
        | ex -> printfn "Error listing clients: %s" ex.Message
    }

// Function to fetch logs for a specific client
let fetchClientLogs (clientId: string) =
    async {
        try
            let! response = httpClient.GetAsync(sprintf "%s/clients/%s" baseUrl clientId) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Logs for client %s:\n%s" clientId content
            else
                printfn "Error: %s" response.ReasonPhrase
        with
        | ex -> printfn "Error fetching logs for client %s: %s" clientId ex.Message
    }

// Function to find and copy a script to the session's currentscript directory in two locations and rename it
let useScript (scriptName: string) (sessionId: string) =
    async {
        try
            let sessionScriptDir = Path.Combine(Directory.GetCurrentDirectory(), $"../Suave Web Server/scripts/currentscript/{sessionId}")
            let debugScriptDir = Path.Combine(Directory.GetCurrentDirectory(), $"../Suave Web Server/bin/Debug/net8.0/scripts/currentscript/{sessionId}")
            let searchPattern = scriptName + ".fsx"

            let files = Directory.GetFiles(libraryPath, searchPattern, SearchOption.AllDirectories)
            if files.Length > 0 then
                let scriptSourcePath = files.[0]
                [sessionScriptDir; debugScriptDir] |> List.iter (fun dir -> 
                    if not (Directory.Exists(dir)) then
                        Directory.CreateDirectory(dir) |> ignore)
                [sessionScriptDir; debugScriptDir] |> List.iter (fun dir ->
                    let destinationPath = Path.Combine(dir, "currentscript.txt")
                    File.Copy(scriptSourcePath, destinationPath, true))
                let relativePath = sprintf "/scripts/currentscript/%s/currentscript.txt" sessionId
                let content = new StringContent(relativePath, System.Text.Encoding.UTF8, "text/plain")
                let! response = httpClient.PostAsync(sprintf "%s/setScript?clientId=%s" baseUrl sessionId, content) |> Async.AwaitTask
                if response.IsSuccessStatusCode then
                    printfn "Script %s is now set as 'currentscript.txt' for session %s in both locations." scriptName sessionId
                else
                    printfn "Error setting script for session %s: %s" sessionId response.ReasonPhrase
            else
                printfn "Script %s not found in library." scriptName
        with
        | ex -> printfn "Error using script %s for session %s: %s" scriptName sessionId ex.Message
    }

// Function to execute a command on a client's session
let executeCommand (command: string) (sessionId: string) =
    async {
        try
            let commandScriptPath = Path.Combine(libraryPath, "command.fsx")
            if File.Exists(commandScriptPath) then
                let sessionScriptDir = Path.Combine(Directory.GetCurrentDirectory(), $"../Suave Web Server/scripts/currentscript/{sessionId}")
                if not (Directory.Exists(sessionScriptDir)) then
                    Directory.CreateDirectory(sessionScriptDir) |> ignore
                let scriptContent = File.ReadAllText(commandScriptPath).Replace("{command provided by user}", command)
                let destinationPath = Path.Combine(sessionScriptDir, "currentscript.txt")
                File.WriteAllText(destinationPath, scriptContent)
                printfn "Command script set for session %s." sessionId
            else
                printfn "Command script file 'command.fsx' not found."
        with
        | ex -> printfn "Error executing command for session %s: %s" sessionId ex.Message
    }

// Function to list scripts in a specific directory
let listScriptsInDirectory (directoryPath: string) =
    let fullDirectoryPath = Path.Combine(libraryPath, directoryPath)
    let files = Directory.GetFiles(fullDirectoryPath, "*.fsx", SearchOption.AllDirectories)
    printfn "Scripts in %s:" directoryPath
    files |> Array.iter (fun file -> printfn "Script: %s" (Path.GetFileName(file)))

// Function to list scripts matching a search query
let listMatchingScripts (query: string) =
    let files = Directory.GetFiles(libraryPath, "*.fsx", SearchOption.AllDirectories)
    let matchedFiles = files |> Array.filter (fun file -> (Path.GetFileName(file).ToLower()).Contains(query.ToLower()))
    if matchedFiles.Length > 0 then
        printfn "Matching Scripts:"
        matchedFiles |> Array.iter (fun file -> printfn "Script: %s" (Path.GetFileName(file)))
    else
        printfn "No matching scripts found."

// Function to handle user input and commands
let handleInput () =
    async {
        while true do
            printfn "Enter command (list / logs <clientId> / use <scriptName> <clientId> / library [<directoryName>] / search <query> / command <command> <sessionId> / exit):"
            let input = Console.ReadLine()
            match input.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) with
            | [| "list" |] ->
                do! listClients ()
            | [| "logs"; clientId |] ->
                do! fetchClientLogs clientId
            | [| "use"; scriptName; clientId |] ->
                do! useScript scriptName clientId
            | [| "library" |] ->
                listScriptsInDirectory ""
            | [| "library"; directoryName |] ->
                listScriptsInDirectory directoryName
            | [| "search"; query |] ->
                listMatchingScripts query
            | [| "command"; command; sessionId |] ->
                do! executeCommand command sessionId
            | [| "exit" |] -> 
                printfn "Exiting..."
                return ()
            | _ -> 
                printfn "Invalid command. Please try again."
    }


// Start log watchers for each session
let startLogWatchers () =
    // Assume you have a mechanism to get session IDs and their script directories
    // For now, it's manual setup:
    let sessionScriptDir = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/currentscript/Session1")
    let debugScriptDir = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/bin/Debug/net8.0/scripts/currentscript/Session1")
    let watcher = createLogWatcher "Session1" sessionScriptDir debugScriptDir
    ()  // Return unit to satisfy F# syntax for 'let' as not the final element in a block


// Entry point
[<EntryPoint>]
let main argv =
    printfn "Welcome to the Management Console"
    startLogWatchers ()  // Start monitoring session logs
    handleInput () |> Async.RunSynchronously
    00
