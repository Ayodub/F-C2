open System
open System.Diagnostics

// Function to get the ARP table
let getArpTable () =
    try
        // Run the arp command to get the ARP table
        let startInfo = ProcessStartInfo(
            FileName = "arp",
            Arguments = "-a",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        )
        
        use proc = new Process()
        proc.StartInfo <- startInfo
        proc.Start() |> ignore
        let output = proc.StandardOutput.ReadToEnd()
        let error = proc.StandardError.ReadToEnd()
        proc.WaitForExit()
        
        if proc.ExitCode = 0 then
            output
        else
            sprintf "Error: %s" error
    with
    | ex -> sprintf "Error: %s" ex.Message

// Function to parse and display the ARP table
let displayArpTable () =
    let arpTable = getArpTable()
    printfn "ARP Table:\n%s" arpTable

// Execute the function to display the ARP table
displayArpTable()
