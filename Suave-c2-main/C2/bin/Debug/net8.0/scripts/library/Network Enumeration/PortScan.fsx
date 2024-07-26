open System
open System.Net
open System.Net.Sockets

// Function to scan a single port on an IP address
let scanPort (ipAddress: string) (port: int) =
    try
        use client = new TcpClient()
        let task = client.ConnectAsync(ipAddress, port)
        task.Wait(1000) // Timeout of 1000 ms
        if client.Connected then
            Some port
        else
            None
    with
    | ex -> 
        printfn "Error scanning port %d on %s: %s" port ipAddress ex.Message
        None

// Function to identify service type for common ports
let identifyService (port: int) =
    match port with
    | 21 -> "FTP"
    | 22 -> "SSH"
    | 23 -> "Telnet"
    | 25 -> "SMTP"
    | 53 -> "DNS"
    | 80 -> "HTTP"
    | 443 -> "HTTPS"
    | 3306 -> "MySQL"
    | 3389 -> "RDP"
    | _ -> "Unknown"

// Function to scan a range of ports on an IP address and display service types for common ports
let scanPorts (ipAddress: string) =
    let commonPorts = [21; 22; 23; 25; 53; 80; 443; 3306; 3389]
    [0..65535]
    |> List.iter (fun port ->
        match scanPort ipAddress port with
        | Some p when List.contains p commonPorts -> 
            printfn "Open port %d on %s: %s" p ipAddress (identifyService p)
        | _ -> ()
    )

// Function to perform a port scan on a subnet
let scanSubnet (subnet: string) (start: int) (end: int) =
    [start..end]
    |> List.map (fun i -> sprintf "%s.%d" subnet i)
    |> List.iter (fun ipAddress ->
        printfn "Scanning %s..." ipAddress
        scanPorts ipAddress
    )

// Example usage
// Replace "192.168.1" with your subnet base
let subnetBase = "192.168.1"  // The base of the subnet (e.g., 192.168.1)
let startRange = 1            // Start of the IP range (e.g., 1)
let endRange = 254            // End of the IP range (e.g., 254)

// Perform the subnet scan
scanSubnet subnetBase startRange endRange
