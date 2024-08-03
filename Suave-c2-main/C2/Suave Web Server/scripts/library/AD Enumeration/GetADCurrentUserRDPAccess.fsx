// currently the script is acting as a wrapper for powershell rather than calling windows APIs from F#. This is because of some dependency considerations which we may resolve later.

open System
open System.Diagnostics

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

// PowerShell command to get all computers in the domain and check Remote Desktop status
let psGetComputersAndRDStatus =
    """
    $computers = Get-ADComputer -Filter * -Property * | Select-Object Name, Enabled
    foreach ($computer in $computers) {
        $RDS = Get-WmiObject -Class Win32_TerminalServiceSetting -ComputerName $computer.Name -Namespace root\CIMV2\TerminalServices
        $userAccess = Get-ADGroupMember 'Remote Desktop Users' | Where-Object {$_.SamAccountName -eq $env:USERNAME}
        [pscustomobject]@{
            ComputerName = $computer.Name
            RemoteDesktopEnabled = $RDS.AllowTSConnections
            CurrentUserHasAccess = $userAccess -ne $null
        }
    }
    """

// Run the PowerShell command and process the output
let rdStatusResults =
    match runPowerShellViaCmd psGetComputersAndRDStatus with
    | Some results -> 
        printfn "Results:\n%s" results
        results.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
    | None -> 
        printfn "Failed to retrieve information."
        Array.empty

// Print the formatted results
rdStatusResults |> Array.iter (fun line ->
    printfn "%s" line
)
