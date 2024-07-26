open System
open System.IO
open System.Net.Http
open System.Threading.Tasks

let baseUrl = "http://192.168.8.107:8000"  // Corrected base URL
let httpClient = new HttpClient()

// Function to list all clients
let listClients () =
    async {
        try
            let! response = httpClient.GetAsync(sprintf "%s/clients" baseUrl) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Clients:\n%s" content
            else
                printfn "Error: %s" response.ReasonPhrase
        with
        | ex -> printfn "Error listing clients: %s" ex.Message
    }

// Function to interact with a specific client
let fetchClientLogs (clientId: string) =
    async {
        try
            let! response = httpClient.GetAsync(sprintf "%s/clients/%s" baseUrl clientId) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Logs for %s:\n%s" clientId content
            else
                printfn "Error: %s" response.ReasonPhrase
        with
        | ex -> printfn "Error fetching logs for client %s: %s" clientId ex.Message
    }

// Function to change the script path for a specific client
let changeScriptPath (clientId: string) (scriptPath: string) =
    async {
        try
            let content = new StringContent(scriptPath, System.Text.Encoding.UTF8, "text/plain")
            let! response = httpClient.PostAsync(sprintf "%s/setScript?clientId=%s" baseUrl clientId, content) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! responseText = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Response:\n%s" responseText
            else
                printfn "Error: %s" response.ReasonPhrase
        with
        | ex -> printfn "Error changing script path for client %s: %s" clientId ex.Message
    }

// Function to list scripts in a specified directory
let rec listScriptsInDirectory (directoryPath: string) (indent: string) =
    let files = Directory.GetFiles(directoryPath)
    let directories = Directory.GetDirectories(directoryPath)

    // List files in the current directory
    for file in files do
        printfn "%sScript: %s" indent (Path.GetFileName(file))

    // List directories and their script counts
    for dir in directories do
        let scriptCount = Directory.GetFiles(dir).Length
        printfn "%sDirectory: %s - %d scripts" indent (Path.GetFileName(dir)) scriptCount
        listScriptsInDirectory dir (indent + "    ")  // Indent for subdirectory

// Function to list all scripts in the library
let listLibraryScripts () =
    let libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/library")
    if Directory.Exists(libraryPath) then
        printfn "Library Scripts:"
        listScriptsInDirectory libraryPath ""
    else
        printfn "Library directory does not exist."

// Function to list scripts in a specific directory within the library
let listScriptsInSpecificDirectory (directoryName: string) =
    let libraryPath = Path.Combine(Directory.GetCurrentDirectory(), "../Suave Web Server/scripts/library", directoryName)
    if Directory.Exists(libraryPath) then
        printfn "Scripts in Directory '%s':" directoryName
        listScriptsInDirectory libraryPath ""
    else
        printfn "Directory '%s' does not exist in the library." directoryName

// Function to handle user input and commands
let handleInput () =
    async {
        while true do
            printfn "Enter command (list / logs <clientId> / set <clientId> <scriptPath> / library [<directoryName>] / exit):"
            let input = Console.ReadLine()
            match input.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) with
            | [| "list" |] ->
                do! listClients ()
            | [| "logs"; clientId |] ->
                do! fetchClientLogs clientId
            | [| "set"; clientId; scriptPath |] ->
                do! changeScriptPath clientId scriptPath
            | [| "library" |] ->
                listLibraryScripts ()
            | [| "library"; directoryName |] ->
                listScriptsInSpecificDirectory directoryName
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
