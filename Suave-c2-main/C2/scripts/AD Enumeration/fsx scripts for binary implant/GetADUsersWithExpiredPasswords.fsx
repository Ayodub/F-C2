#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.ActiveDirectory

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    let domainName = domain.Name
    domainName

let getUsersWithExpiredPasswords (domainName: string) =
    let entry = new DirectoryEntry(sprintf "LDAP://%s" domainName)
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(&(objectCategory=person)(objectClass=user)(msDS-UserPasswordExpiryTimeComputed<=1))"
    searcher.PropertiesToLoad.Add("name") |> ignore
    
    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result -> 
        result.Properties.["name"].[0].ToString()
    )
    |> Seq.toList

let domainName = getCurrentDomainName()
let usersWithExpiredPasswords = getUsersWithExpiredPasswords(domainName)
usersWithExpiredPasswords |> List.iter (printfn "User with expired password: %s")
