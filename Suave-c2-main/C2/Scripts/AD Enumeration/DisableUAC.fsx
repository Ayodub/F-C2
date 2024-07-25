open System
open Microsoft.Win32

// Function to modify the registry to disable UAC prompts for administrators
let disableUAC () =
    try
        // Path to the registry key
        let keyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
        
        // Open the registry key
        use key = Registry.LocalMachine.OpenSubKey(keyPath, true)
        
        if key <> null then
            // Set the value to disable UAC prompts
            key.SetValue("EnableLUA", 0, RegistryValueKind.DWord)
            key.SetValue("ConsentPromptBehaviorAdmin", 0, RegistryValueKind.DWord)
            printfn "UAC settings have been modified to disable prompts for administrators."
        else
            printfn "Failed to open registry key."
    with
    | ex -> printfn "Error: %s" ex.Message

// Execute the function to disable UAC
disableUAC()
