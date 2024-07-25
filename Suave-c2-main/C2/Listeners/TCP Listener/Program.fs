open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading.Tasks

// Function to handle incoming TCP client connections
let handleClient (client: TcpClient) =
    async {
        try
            use stream = client.GetStream()
            let buffer = Array.create 1024 (byte 0)  // Buffer to hold incoming data
            let rec readData () =
                async {
                    let! bytesRead = stream.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask
                    if bytesRead > 0 then
                        let message = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                        printfn "Received: %s" message
                        // Echo the message back to the client
                        let response = "Received: " + message
                        let responseBytes = Encoding.UTF8.GetBytes(response)
                        do! stream.WriteAsync(responseBytes, 0, responseBytes.Length) |> Async.AwaitTask
                        return! readData ()  // Continue reading data
                    else
                        printfn "Client disconnected."
                }
            do! readData ()  // Start reading data
        with
        | ex -> printfn "Error handling client: %s" ex.Message
    }

// Function to start the TCP listener
let startListener (port: int) =
    let listener = new TcpListener(IPAddress.Any, port)
    listener.Start()
    printfn "Listening on port %d" port

    while true do
        try
            let client = listener.AcceptTcpClient()
            printfn "Client connected."
            Async.Start (handleClient client)
        with
        | ex -> printfn "Error accepting client: %s" ex.Message

// Start the TCP listener on port 4444
[<EntryPoint>]
let main argv =
    startListener 4444
    0 // return an integer exit code
