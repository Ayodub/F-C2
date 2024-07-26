#r "Microsoft.Win32.Registry.dll"

open System
open Microsoft.Win32

// Function to add an exclusion path to Windows Defender
let addDefenderExclusion (path: string) =
    let registryKeyPath = @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths"
    use key = Registry.LocalMachine.CreateSubKey(registryKeyPath)
    
    if key = null then
        printfn "Failed to open registry key: %s" registryKeyPath
    else
        // Generate a unique name for the exclusion path
        let exclusionName = sprintf "Path_%d" (key.SubKeyCount + 1)
        key.SetValue(exclusionName, path, RegistryValueKind.String)
        printfn "Added exclusion: %s" path

// Example usage: Add a folder to Windows Defender exclusions
let folderToExclude = @"C:\Path\To\Exclude"
addDefenderExclusion folderToExclude
