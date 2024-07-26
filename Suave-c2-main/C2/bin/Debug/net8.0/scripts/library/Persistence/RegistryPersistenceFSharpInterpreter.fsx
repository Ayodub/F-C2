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

// Function to add an F# script to the registry for auto-start
let addFSharpStartupEntry (keyPath: string) (name: string) (scriptPath: string) =
    try
        // Escape double quotes in the script path
        let escapedScriptPath = scriptPath.Replace("\"", "\\\"")
        // Construct the command to add the registry key
        let command = "reg"
        let arguments = sprintf @"add %s /v %s /d \"dotnet fsi \\\"%s\\\"\" /f" keyPath name escapedScriptPath
        let result = runCommand command arguments
        printfn "Registry Update Result: %s" result
    with
    | ex -> printfn "Error: %s" ex.Message

// Example usage
// Replace "MyFSharpScript" with the desired name and "C:\Path\To\YourScript.fsx" with the path to the F# script
let keyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run"  // Use HKLM for all users
let name = "MyFSharpScript"  // Change this to the desired name
let scriptPath = @"C:\Path\To\YourScript.fsx"  // Replace with the path to your F# script

// Call the function to add the F# script to startup
addFSharpStartupEntry keyPath name scriptPath
