open System
open System.IO
open System.Net.Http
open System.Threading.Tasks

let baseUrl = "http://192.168.8.107:8000"  // Corrected base URL
let httpClient = new HttpClient()
let libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/library")

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
            // Define the base paths
            let libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/library")
            let sessionScriptDir = Path.Combine(Directory.GetCurrentDirectory(), $"../Suave Web Server/scripts/currentscript/{sessionId}")
            let debugScriptDir = Path.Combine(Directory.GetCurrentDirectory(), $"../Suave Web Server/bin/Debug/net8.0/scripts/currentscript/{sessionId}")
            let searchPattern = scriptName + ".fsx"

            // Search recursively for the script
            let files = Directory.GetFiles(libraryPath, searchPattern, SearchOption.AllDirectories)
            if files.Length > 0 then
                let scriptSourcePath = files.[0]

                // Ensure directories exist
                let directories = [sessionScriptDir; debugScriptDir]
                directories |> List.iter (fun dir -> 
                    if not (Directory.Exists(dir)) then
                        Directory.CreateDirectory(dir) |> ignore)

                // Copy the script as a .txt file to both locations, renaming it to 'currentscript.txt'
                directories |> List.iter (fun dir ->
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
        | ex -> 
            printfn "Error using script %s for session %s: %s" scriptName sessionId ex.Message
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
            printfn "Enter command (list / logs <clientId> / use <scriptName> <clientId> / library [<directoryName>] / search <query> / exit):"
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
            | [| "exit" |] -> 
                printfn "Exiting..."
                return ()
            | _ -> 
                printfn "Invalid command. Please try again."
    }

// Entry point
[<EntryPoint>]
let main argv =
    printfn "Welcome to the Management Console"
    handleInput () |> Async.RunSynchronously
    0
