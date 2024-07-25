#r "Microsoft.Win32.Registry.dll"

open System
open Microsoft.Win32

let checkAlwaysInstallElevated () =
    let registryPaths = [
        @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\AlwaysInstallElevated"
        @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\Explorer\AlwaysInstallElevated"
    ]
    
    registryPaths
    |> List.map (fun path ->
        let key = Registry.LocalMachine.OpenSubKey(path)
        match key with
        | null -> (path, false) // Key does not exist
        | _ ->
            let value = key.GetValue("AlwaysInstallElevated")
            let isEnabled = value <> null && (value :?> int) = 1
            (path, isEnabled)
    )

let results = checkAlwaysInstallElevated()
results |> List.iter (fun (path, isEnabled) ->
    if isEnabled then
        printfn "The 'Always Install Elevated' setting is enabled at: %s" path
    else
        printfn "The 'Always Install Elevated' setting is disabled or not found at: %s" path
)
