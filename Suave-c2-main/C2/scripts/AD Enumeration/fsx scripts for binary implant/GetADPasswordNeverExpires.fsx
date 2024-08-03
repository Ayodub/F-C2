#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.ActiveDirectory

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    let domainName = domain.Name
    domainName

let getUsersWithPasswordNeverExpires (domainName: string) =
    let entry = new DirectoryEntry(sprintf "LDAP://%s" domainName)
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(&(objectCategory=person)(objectClass=user)(userAccountControl:1.2.840.113556.1.4.803:=65536))"
    searcher.PropertiesToLoad.Add("name") |> ignore
    
    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result -> 
        result.Properties.["name"].[0].ToString()
    )
    |> Seq.toList

let domainName = getCurrentDomainName()
let usersWithPasswordNeverExpires = getUsersWithPasswordNeverExpires(domainName)
usersWithPasswordNeverExpires |> List.iter (printfn "User with password never expires: %s")
