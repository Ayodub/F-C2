#r "Microsoft.Win32.Registry.dll"

open System
open Microsoft.Win32

let checkAdminShares () =
    let keyPath = @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"
    use key = Registry.LocalMachine.OpenSubKey(keyPath)
    
    match key with
    | null -> "Registry key not found"
    | _ ->
        let adminSharesDisabled = key.GetValue("AutoShareWks") = null
        if adminSharesDisabled then
            "Admin shares are disabled"
        else
            "Admin shares are enabled"

let result = checkAdminShares()
printfn "%s" result
