#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.AccountManagement
open System.DirectoryServices.ActiveDirectory

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    let domainName = domain.Name
    domainName

let getADComputers (domainName: string) =
    let entry = new DirectoryEntry(sprintf "LDAP://%s" domainName)
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(objectClass=computer)"
    searcher.PropertiesToLoad.Add("name") |> ignore
    
    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result -> result.Properties.["name"].[0].ToString())
    |> Seq.toList

let domainName = getCurrentDomainName()
let computers = getADComputers(domainName)
computers |> List.iter (printfn "%s")
