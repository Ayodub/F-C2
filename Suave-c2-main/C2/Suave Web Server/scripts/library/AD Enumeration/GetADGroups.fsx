// currently the script is acting as a wrapper for powershell rather than calling windows APIs from F#. This is because of some dependency considerations which we may resolve later.

open System.Diagnostics
open System

// Function to run PowerShell commands via cmd.exe and return its output
let runPowerShellViaCmd (psCommand: string) =
    try
        let command = sprintf "/c powershell -Command \"%s\"" psCommand
        let startInfo = ProcessStartInfo(
            FileName = "cmd.exe",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        )
        use cmdProcess = new Process()
        cmdProcess.StartInfo <- startInfo
        cmdProcess.Start() |> ignore
        cmdProcess.WaitForExit()

        let output = cmdProcess.StandardOutput.ReadToEnd()
        let error = cmdProcess.StandardError.ReadToEnd()

        if cmdProcess.ExitCode = 0 then Some output
        else Some (sprintf "Error: %s" error)
    with
    | ex -> Some (sprintf "Exception: %s" ex.Message)

// PowerShell command to get Active Directory groups in the current domain
let psGetADGroups =
    """
    Import-Module ActiveDirectory
    Get-ADGroup -Filter * | Select-Object Name | Format-Table -AutoSize | Out-String
    """

// Run the PowerShell command and print the output
match runPowerShellViaCmd psGetADGroups with
| Some results -> 
    printfn "Active Directory Groups:\n%s" results
    results.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
    |> Array.iter (fun line -> printfn "%s" line)
| None -> 
    printfn "Failed to retrieve AD groups."
