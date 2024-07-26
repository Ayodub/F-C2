#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.ActiveDirectory
open System.DirectoryServices.AccountManagement

let getCurrentDomainName () =
    let domain = Domain.GetCurrentDomain()
    let domainName = domain.Name
    domainName

let getUsersInGroup (groupName: string) =
    use context = new PrincipalContext(ContextType.Domain)
    let group = GroupPrincipal.FindByIdentity(context, groupName)
    group.GetMembers(true)
    |> Seq.cast<Principal>
    |> Seq.map (fun user -> user.SamAccountName)
    |> Seq.toList

let domainName = getCurrentDomainName()
let domainAdmins = getUsersInGroup("Domain Admins")
domainAdmins |> List.iter (printfn "Domain Admin: %s")
