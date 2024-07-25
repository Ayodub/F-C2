open System
open System.IO
open System.Net
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Files
open Suave.RequestErrors

// Define the root directory to serve files from
let rootDirectory = AppDomain.CurrentDomain.BaseDirectory

// Function to log received data
let logData (data: string) =
    try
        let logFile = Path.Combine(rootDirectory, "server_log.txt")
        File.AppendAllText(logFile, $"{DateTime.Now}: {data}{Environment.NewLine}")
    with
    | ex -> printfn "Error logging data: %s" ex.Message

// Define the web server configuration
let config =
    { defaultConfig with
        bindings = [ HttpBinding.create HTTP IPAddress.Any 8000us ] }

// Define the web app
let app =
    choose [
        GET >=> choose [
            path "/" >=> file (System.IO.Path.Combine(rootDirectory, "index.html"))
            path "/flag.txt" >=> file (System.IO.Path.Combine(rootDirectory, "flag.txt"))
            path "/ARPScanner.txt" >=> file (System.IO.Path.Combine(rootDirectory, "ARPScanner.txt"))
            browseHome
        ]
        POST >=> choose [
            path "/receiveOutput" >=> request (fun r ->
                let data = r.rawForm
                let dataString = System.Text.Encoding.UTF8.GetString(data)
                logData dataString
                OK "Data received and logged"
            )
        ]
    ]

// Start the web server
startWebServer config app
