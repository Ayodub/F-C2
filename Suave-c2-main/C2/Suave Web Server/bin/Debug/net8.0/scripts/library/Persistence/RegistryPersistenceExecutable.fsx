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

// Function to add a program to the registry for auto-start
let addStartupEntry (keyPath: string) (name: string) (executablePath: string) =
    try
        // Construct the command to add the registry key
        let command = "reg"
        let arguments = sprintf @"add %s /v %s /d \"%s\" /f" keyPath name executablePath
        let result = runCommand command arguments
        printfn "Registry Update Result: %s" result
    with
    | ex -> printfn "Error: %s" ex.Message

// Example usage
// Replace "MyApp" with the desired name and "C:\Path\To\YourApp.exe" with the path to the executable
let keyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run"  // Use HKLM for all users
let name = "MyApp"  // Change this to the desired name
let executablePath = @"C:\Path\To\YourApp.exe"  // Replace with the path to the executable

// Call the function to add the startup entry
addStartupEntry keyPath name executablePath
