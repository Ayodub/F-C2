#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.ActiveDirectory

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    let domainName = domain.Name
    domainName

let KerberoastableUsers (domainName: string) =
    let entry = new DirectoryEntry(sprintf "LDAP://%s" domainName)
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(&(objectCategory=person)(objectClass=user)(servicePrincipalName=*))"
    searcher.PropertiesToLoad.Add("name") |> ignore
    searcher.PropertiesToLoad.Add("servicePrincipalName") |> ignore
    
    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result -> 
        let userName = result.Properties.["name"].[0].ToString()
        let spn = result.Properties.["servicePrincipalName"]
        userName, spn
    )
    |> Seq.toList

let domainName = getCurrentDomainName()
let UsersList = KerberoastableUsers(domainName)
UsersList 
|> List.iter (fun (userName, spn) -> 
    printfn "User: %s" userName
    printfn "SPNs: %A" spn
)
