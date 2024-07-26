#r "System.DirectoryServices.dll"

open System
open System.DirectoryServices

// Function to get domain trusts
let getDomainTrusts () =
    let domainRoot = "LDAP://RootDSE"
    use rootEntry = new DirectoryEntry(domainRoot)
    let domainController = rootEntry.Properties.["defaultNamingContext"].Value.ToString()
    let searchRoot = sprintf "LDAP://%s" domainController
    use entry = new DirectoryEntry(searchRoot)
    
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(objectClass=trustedDomain)"
    searcher.PropertiesToLoad.Add("name") |> ignore
    searcher.PropertiesToLoad.Add("trustType") |> ignore
    searcher.PropertiesToLoad.Add("trustDirection") |> ignore
    
    let results = searcher.FindAll()
    results
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result ->
        let name = result.Properties.["name"].[0].ToString()
        let trustType = result.Properties.["trustType"].[0].ToString()
        let trustDirection = result.Properties.["trustDirection"].[0].ToString()
        (name, trustType, trustDirection)
    )
    |> Seq.toList

// Print domain trusts and their configurations
let printDomainTrusts () =
    let trusts = getDomainTrusts()
    trusts
    |> List.iter (fun (name, trustType, trustDirection) ->
        printfn "Domain: %s" name
        printfn "Trust Type: %s" trustType
        printfn "Trust Direction: %s" trustDirection
        printfn "----------------------------"
    )

printDomainTrusts()
