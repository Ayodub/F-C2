open System
open Microsoft.Win32
open System.Diagnostics

// Function to check cached RDP sessions from the registry
let checkCachedRDPSessions () =
    try
        let keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Terminal Server Client\Default"
        use key = Registry.CurrentUser.OpenSubKey(keyPath)
        
        if key <> null then
            // List all subkeys (which might represent cached RDP sessions)
            let subKeys = key.GetSubKeyNames()
            if subKeys.Length > 0 then
                for subKey in subKeys do
                    let sessionKey = key.OpenSubKey(subKey)
                    let server = sessionKey.GetValue("HostName", "Unknown" :> obj) :?> string
                    let username = sessionKey.GetValue("UserNameHint", "Unknown" :> obj) :?> string
                    
                    printfn "RDP Session:"
                    printfn "  Server: %s" server
                    printfn "  Username: %s" username
                    printfn "---------------------------"
            else
                printfn "No cached RDP sessions found."
        else
            printfn "Failed to open registry key."
    with
    | ex -> printfn "Error: %s" ex.Message

// Function to check Windows Event Logs for RDP session details
let checkEventLogs () =
    try
        // Use wevtutil to query event logs
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "wevtutil.exe"
        startInfo.Arguments <- "qe Security /q:\"*[System[Provider[@Name='Microsoft-Windows-TerminalServices-LocalSessionManager']]]\" /f:text /c:10"
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.CreateNoWindow <- true

        use proc = Process.Start(startInfo)
        let output = proc.StandardOutput.ReadToEnd()
        proc.WaitForExit()

        printfn "Recent RDP Session Logs:"
        printfn "%s" output
    with
    | ex -> printfn "Error: %s" ex.Message

// Execute checks
printfn "Checking cached RDP sessions..."
checkCachedRDPSessions()

printfn "Checking RDP session logs..."
checkEventLogs()
