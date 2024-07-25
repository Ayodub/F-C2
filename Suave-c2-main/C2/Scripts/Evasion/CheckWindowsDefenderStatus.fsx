#r "System.Management.dll"

open System
open System.Management

let checkDefenderStatus () =
    let query = "SELECT * FROM AntiVirusProduct"
    use searcher = new ManagementObjectSearcher("root\\SecurityCenter2", query)
    
    let results = searcher.Get()
    if results.Count > 0 then
        "Windows Defender is enabled"
    else
        "Windows Defender is not enabled"

let result = checkDefenderStatus()
printfn "%s" result
