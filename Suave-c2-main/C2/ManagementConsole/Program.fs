open System
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
let interactWithClient (clientId: string) =
    async {
        try
            let! response = httpClient.GetAsync(sprintf "%s/clients/%s" baseUrl clientId) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Client %s:\n%s" clientId content
            else
                printfn "Error: %s" response.ReasonPhrase
        with
        | ex -> printfn "Error interacting with client %s: %s" clientId ex.Message
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

// Function to handle user input and commands
let handleInput () =
    async {
        while true do
            printfn "Enter command (list / interact <clientId> / set <clientId> <scriptPath> / exit):"
            let input = Console.ReadLine()
            match input.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries) with
            | [| "list" |] ->
                do! listClients ()
            | [| "interact"; clientId |] ->
                do! interactWithClient clientId
            | [| "set"; clientId; scriptPath |] ->
                do! changeScriptPath clientId scriptPath
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
