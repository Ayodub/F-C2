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

// Function to add a new user
let addUser (username: string) (password: string) =
    try
        let command = "net"
        let arguments = sprintf "user %s %s /add" username password
        let result = runCommand command arguments
        printfn "Add User Result: %s" result
    with
    | ex -> printfn "Error: %s" ex.Message

// Example usage

let username = "newuser"  // Change this to the desired username
let password = "newpassword"  // Change this to the desired password

// Call the function to add the new user
addUser username password
