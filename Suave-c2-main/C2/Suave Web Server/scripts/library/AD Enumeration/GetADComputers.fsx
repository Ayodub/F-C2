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

// PowerShell command to retrieve all computers from the AD
let adComputersCommand = 
    """
    Get-ADComputer -Filter * | Select-Object -ExpandProperty Name
    """

// Running the PowerShell command from F#
let domainComputersOutput =
    match runPowerShellViaCmd adComputersCommand with
    | Some result -> 
        printfn "Computers in the domain:\n%s" result
        result.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
    | None -> 
        printfn "Failed to retrieve computers."
        Array.empty

// Print each computer's name
domainComputersOutput |> Array.iter (printfn "Computer: %s")

