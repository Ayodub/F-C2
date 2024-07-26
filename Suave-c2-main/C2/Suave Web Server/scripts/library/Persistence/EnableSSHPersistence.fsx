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

// Function to enable SSH
let enableSSH () =
    try
        // Install OpenSSH Server feature
        let installCommand = "powershell"
        let installArguments = "Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0"
        let installResult = runCommand installCommand installArguments
        printfn "OpenSSH Installation Result: %s" installResult
        
        // Start the SSH service
        let startServiceCommand = "sc"
        let startServiceArguments = "start sshd"
        let startServiceResult = runCommand startServiceCommand startServiceArguments
        printfn "SSH Service Start Result: %s" startServiceResult
        
        // Set SSH service to start automatically
        let setServiceCommand = "sc"
        let setServiceArguments = "config sshd start=auto"
        let setServiceResult = runCommand setServiceCommand setServiceArguments
        printfn "SSH Service Configuration Result: %s" setServiceResult

        // Allow SSH through Windows Firewall
        let firewallCommand = "netsh"
        let firewallArguments = "advfirewall firewall add rule name=\"OpenSSH Server (TCP-In)\" dir=in action=allow protocol=TCP localport=22"
        let firewallResult = runCommand firewallCommand firewallArguments
        printfn "Firewall Update Result: %s" firewallResult
    with
    | ex -> printfn "Error: %s" ex.Message

// Call the function to enable SSH
enableSSH()
