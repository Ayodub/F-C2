#r "System.DirectoryServices.dll"
#r "System.DirectoryServices.AccountManagement.dll"
#r "System.Management.dll"

open System
open System.DirectoryServices
open System.DirectoryServices.AccountManagement
open System.Management

// Function to get all computers in the domain
let getAllComputers () =
    let domain = Domain.GetCurrentDomain()
    let directoryEntry = new DirectoryEntry(sprintf "LDAP://%s" domain.Name)
    let searcher = new DirectorySearcher(directoryEntry)
    searcher.Filter <- "(objectClass=computer)"
    searcher.PropertiesToLoad.Add("name") |> ignore

    searcher.FindAll()
    |> Seq.cast<SearchResult>
    |> Seq.map (fun result -> result.Properties.["name"].[0].ToString())
    |> Seq.toList

// Function to check Remote Desktop status on a computer
let checkRemoteDesktopOnComputer (computerName: string) =
    let scope = new ManagementScope(sprintf @"\\%s\root\cimv2" computerName, ConnectionOptions())
    try
        scope.Connect()
        let query = new ObjectQuery("SELECT * FROM Win32_TerminalServiceSetting")
        let searcher = new ManagementObjectSearcher(scope, query)
        let result = searcher.Get()
        
        let isEnabled = 
            result
            |> Seq.cast<ManagementObject>
            |> Seq.exists (fun obj -> obj["AllowTSConnections"] = 1)
        
        isEnabled
    with
    | _ -> false

// Function to check if the current user is a member of 'Remote Desktop Users' group on a computer
let checkUserAccessOnComputer (computerName: string) =
    let context = new PrincipalContext(ContextType.Domain)
    let group = GroupPrincipal.FindByIdentity(context, "Remote Desktop Users")
    
    let user = UserPrincipal.Current
    if group = null then
        false
    else
        group.Members
        |> Seq.exists (fun principal -> principal.Sid.ToString() = user.Sid.ToString())

// Check Remote Desktop access for all computers
let checkRemoteDesktopAccessForAllComputers () =
    let computers = getAllComputers()
    computers
    |> List.map (fun computerName ->
        let remoteDesktopEnabled = checkRemoteDesktopOnComputer computerName
        let userHasAccess = if remoteDesktopEnabled then checkUserAccessOnComputer computerName else false
        
        (computerName, remoteDesktopEnabled, userHasAccess)
    )

let results = checkRemoteDesktopAccessForAllComputers()
results |> List.iter (fun (computerName, isEnabled, userHasAccess) ->
    if isEnabled then
        if userHasAccess then
            printfn "Computer: %s - Remote Desktop is enabled and current user has access" computerName
        else
            printfn "Computer: %s - Remote Desktop is enabled but current user does not have access" computerName
    else
        printfn "Computer: %s - Remote Desktop is disabled" computerName
)
