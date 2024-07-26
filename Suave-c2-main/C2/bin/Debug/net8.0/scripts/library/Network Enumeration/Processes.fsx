open System
open System.Diagnostics

// Function to list all running processes
let listRunningProcesses () =
    try
        // Get all processes
        let processes = Process.GetProcesses()
        
        // Print the process details
        processes 
        |> Array.iter (fun proc ->
            printfn "ID: %d | Name: %s | Memory: %d KB" 
                proc.Id 
                proc.ProcessName 
                (proc.WorkingSet64 / 1024L)
        )
    with
    | ex -> printfn "Error: %s" ex.Message

// Execute the function to list processes
listRunningProcesses()
