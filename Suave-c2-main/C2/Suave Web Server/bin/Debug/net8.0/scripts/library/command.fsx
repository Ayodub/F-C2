open System
open System.Diagnostics

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
        
        use process = new Process()
        process.StartInfo <- startInfo
        process.Start() |> ignore

        let output = process.StandardOutput.ReadToEnd()
        let error = process.StandardError.ReadToEnd()
        process.WaitForExit()

        if process.ExitCode = 0 then
            Some output
        else
            Some (sprintf "Error: %s" error)
    with
    | ex -> Some (sprintf "Exception: %s" ex.Message)

// Command to run
let command = "{command provided by user}"

// Run the command and print the result
match runCommand command with
| Some result -> printfn "Command Output:\n%s" result
| None -> printfn "Failed to run the command."
