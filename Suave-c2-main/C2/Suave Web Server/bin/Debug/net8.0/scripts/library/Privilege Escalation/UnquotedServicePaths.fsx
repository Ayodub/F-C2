#r "System.ServiceProcess.dll"
#r "Microsoft.Win32.Registry.dll"

open System
open System.ServiceProcess
open Microsoft.Win32

let checkUnquotedServicePaths () =
    let services = ServiceController.GetServices()
    
    services
    |> Seq.filter (fun service ->
        try
            let keyPath = sprintf @"SYSTEM\CurrentControlSet\Services\%s" service.ServiceName
            use key = Registry.LocalMachine.OpenSubKey(keyPath)
            match key with
            | null -> false // Skip services without registry key
            | _ ->
                let imagePath = key.GetValue("ImagePath").ToString()
                // Check if the path contains spaces and is not quoted
                imagePath.Contains(" ") && (not (imagePath.StartsWith("\"") && imagePath.EndsWith("\"")))
        with
        | :? System.NullReferenceException -> false // Skip services without ImagePath
        | _ -> false
    )
    |> Seq.map (fun service ->
        let keyPath = sprintf @"SYSTEM\CurrentControlSet\Services\%s" service.ServiceName
        use key = Registry.LocalMachine.OpenSubKey(keyPath)
        let imagePath = key.GetValue("ImagePath").ToString()
        service.ServiceName, imagePath
    )
    |> Seq.toList

// Run the function and print results
let unquotedServices = checkUnquotedServicePaths()
unquotedServices |> List.iter (fun (serviceName, imagePath) ->
    printfn "Service: %s, ImagePath: %s" serviceName imagePath
)
