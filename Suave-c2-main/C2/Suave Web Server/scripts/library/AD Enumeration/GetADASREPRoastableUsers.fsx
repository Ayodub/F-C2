// currently the script is acting as a wrapper for powershell rather than calling windows APIs from F#. This is because of some dependency considerations which we may resolve later.

open System
open System.Diagnostics

// Function to run a PowerShell command via cmd.exe and return its output
let runPowerShellViaCmd (psCommand: string) =
    try
        // Constructing the command to run PowerShell from cmd.exe
        let command = sprintf "/c powershell -Command \"%s\"" psCommand

        // Setting up the process start information
        let startInfo = ProcessStartInfo(
            FileName = "cmd.exe",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        )
        
        // Executing the command
        use cmdProcess = new Process()
        cmdProcess.StartInfo <- startInfo
        cmdProcess.Start() |> ignore
        cmdProcess.WaitForExit()

        // Gathering output
        let output = cmdProcess.StandardOutput.ReadToEnd()
        let error = cmdProcess.StandardError.ReadToEnd()

        // Returning the output and error
        if cmdProcess.ExitCode = 0 then
            Some output
        else
            Some (sprintf "Error: %s" error)
    with
    | ex -> Some (sprintf "Exception: %s" ex.Message)

// PowerShell command to find users at risk of AS-REP Roasting
let asreproastCheckCommand = 
    """
    Import-Module ActiveDirectory
    Get-ADUser -Filter {UserAccountControl -band 4194304} -Properties Name, UserAccountControl |
    Select-Object Name, @{Name='ASREPRoastable'; Expression={$_.UserAccountControl -band 4194304}}
    """

// Run the command and print the result
match runPowerShellViaCmd asreproastCheckCommand with
| Some result -> printfn "Command Output:\n%s" result
| None -> printfn "Failed to run the command."


