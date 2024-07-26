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

// Function to enable RDP
let enableRDP () =
    try
        // Enable RDP in the registry
        let regCommand = "reg"
        let regArguments = @"add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server"" /v fDenyTSConnections /t REG_DWORD /d 0 /f"
        let regResult = runCommand regCommand regArguments
        printfn "Registry Update Result: %s" regResult
        
        // Start the RDP service
        let serviceCommand = "sc"
        let serviceArguments = "start TermService"
        let serviceResult = runCommand serviceCommand serviceArguments
        printfn "Service Start Result: %s" serviceResult
        
        // Allow RDP through Windows Firewall
        let firewallCommand = "netsh"
        let firewallArguments = "advfirewall firewall set rule group=\"remote desktop\" new enable=Yes"
        let firewallResult = runCommand firewallCommand firewallArguments
        printfn "Firewall Update Result: %s" firewallResult
    with
    | ex -> printfn "Error: %s" ex.Message

// Call the function to enable RDP
enableRDP()
