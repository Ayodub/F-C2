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
    with
    | ex -> sprintf "Error: %s" ex.Message

// Function to parse and display the ARP table
let displayArpTable () =
    let arpTable = getArpTable()
    printfn "ARP Table:\n%s" arpTable

// Execute the function to display the ARP table
displayArpTable()
