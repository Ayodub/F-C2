open System
open System.Net
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Files

// Define the root directory to serve files from
let rootDirectory = AppDomain.CurrentDomain.BaseDirectory

// Define the web server configuration
let config =
    { defaultConfig with
        bindings = [ HttpBinding.create HTTP IPAddress.Any 8000us ] }

// Define the web app
let app =
    choose [
        GET >=> choose [
            path "/" >=> file (System.IO.Path.Combine(rootDirectory, "index.html"))
            browseHome
        ]
    ]

// Start the web server
startWebServer config app
