open System
open System.Net.NetworkInformation

// Function to ping an IP address
let pingIpAddress (ipAddress: string) =
    try
        use ping = new Ping()
        let reply = ping.Send(ipAddress, 1000) // Timeout of 1000 ms
        match reply.Status with
        | IPStatus.Success -> Some(ipAddress)
        | _ -> None
    with
    | ex -> 
        printfn "Error pinging %s: %s" ipAddress ex.Message
        None

// Function to perform a ping sweep on a subnet
let pingSweep (subnet: string) (start: int) (end: int) =
    [start..end]
    |> List.map (fun i -> sprintf "%s.%d" subnet i)
    |> List.iter (fun ipAddress ->
        match pingIpAddress ipAddress with
        | Some(addr) -> printfn "Responsive: %s" addr
        | None -> ()
    )

// Example usage
// Replace "192.168.1" with your subnet base, and adjust the range as needed
let subnetBase = "192.168.1"  // The base of the subnet (e.g., 192.168.1)
let startRange = 1            // Start of the IP range (e.g., 1)
let endRange = 254            // End of the IP range (e.g., 254)

// Perform the ping sweep
pingSweep subnetBase startRange endRange
