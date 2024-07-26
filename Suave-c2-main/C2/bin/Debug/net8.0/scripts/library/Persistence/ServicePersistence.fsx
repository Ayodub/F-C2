open System
open System.Diagnostics

// Function to run a command and return its output
let runCommand (command: string) (arguments: string) =
    let startInfo = ProcessStartInfo(
        FileName = command,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    )
    
    use process = new Process()
    process.StartInfo <- startInfo
    process.Start() |> ignore
    let output = process.StandardOutput.ReadToEnd()
    let error = process.StandardError.ReadToEnd()
    process.WaitForExit()
    
    if process.ExitCode = 0 then
        output
    else
        sprintf "Error: %s" error

// Function to configure a service to start automatically
let configureService (serviceName: string) =
    try
        // Set the service to start automatically
        let command = "sc"
        let arguments = sprintf "config %s start=auto" serviceName
        let result = runCommand command arguments
        printfn "Service Configuration Result: %s" result
    with
    | ex -> printfn "Error: %s" ex.Message

// Example usage
// Replace "YourServiceName" with the actual name of the service you want to configure
let serviceName = "YourServiceName"  // Change this to the name of the service

// Call the function to configure the service
configureService serviceName
