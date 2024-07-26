#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.AccountManagement
open System.DirectoryServices.ActiveDirectory
open System.Security.Principal

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    let domainName = domain.Name
    domainName

let getCurrentUserName () =
    let currentUser = WindowsIdentity.GetCurrent().Name
    currentUser

let getGroupsCurrentUserIsAdminOf (domainName: string, currentUserName: string) =
    let entry = new DirectoryEntry(sprintf "LDAP://%s" domainName)
    let searcher = new DirectorySearcher(entry)
    searcher.Filter <- "(objectClass=group)"
    searcher.PropertiesToLoad.Add("name") |> ignore
    searcher.PropertiesToLoad.Add("member") |> ignore

    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.filter (fun result ->
        result.Properties.["member"]
        |> Seq.cast<string>
        |> Seq.exists (fun member -> member.Contains(currentUserName)))
    |> Seq.map (fun result -> result.Properties.["name"].[0].ToString())
    |> Seq.toList

let domainName = getCurrentDomainName()
let currentUserName = getCurrentUserName()
let groups = getGroupsCurrentUserIsAdminOf(domainName, currentUserName)
groups |> List.iter (printfn "%s")
