#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.ActiveDirectory
open System.DirectoryServices.AccountManagement

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    domain.Name

let getDelegationPermissions (domainName: string) =
    let entry = new DirectoryEntry(sprintf "LDAP://%s" domainName)
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(&(objectCategory=serviceConnectionPoint)(msDS-AllowedToActOnBehalfOfOtherIdentity=*))"
    searcher.PropertiesToLoad.Add("msDS-AllowedToActOnBehalfOfOtherIdentity") |> ignore
    searcher.PropertiesToLoad.Add("distinguishedName") |> ignore

    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result -> 
        let allowedIdentities = result.Properties.["msDS-AllowedToActOnBehalfOfOtherIdentity"]
        let distinguishedName = result.Properties.["distinguishedName"].[0].ToString()
        distinguishedName, allowedIdentities
    )
    |> Seq.toList

let domainName = getCurrentDomainName()
let delegationPermissions = getDelegationPermissions(domainName)
delegationPermissions |> List.iter (fun (distinguishedName, allowedIdentities) ->
    printfn "Distinguished Name: %s" distinguishedName
    allowedIdentities
    |> Seq.cast<obj>
    |> Seq.iter (fun identity ->
        printfn "  Allowed Identity: %s" (identity.ToString())
    )
)
