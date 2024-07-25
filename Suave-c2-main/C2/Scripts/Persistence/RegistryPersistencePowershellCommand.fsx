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

// Function to add a PowerShell command to the registry for auto-start
let addPowerShellStartupEntry (keyPath: string) (name: string) (script: string) =
    try
        // Escape double quotes in the PowerShell script
        let escapedScript = script.Replace("\"", "\\\"")
        // Construct the command to add the registry key
        let command = "reg"
        let arguments = sprintf @"add %s /v %s /d \"powershell.exe -NoProfile -ExecutionPolicy Bypass -Command \\\"%s\\\"\" /f" keyPath name escapedScript
        let result = runCommand command arguments
        printfn "Registry Update Result: %s" result
    with
    | ex -> printfn "Error: %s" ex.Message

// Example usage
// Replace "MyPowerShellScript" with the desired name and the script content
let keyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run"  // Use HKLM for all users
let name = "MyPowerShellScript"  // Change this to the desired name
let script = "Write-Output 'Hello, World!'"  // Replace with your PowerShell command

// Call the function to add the PowerShell command to startup
addPowerShellStartupEntry keyPath name script
