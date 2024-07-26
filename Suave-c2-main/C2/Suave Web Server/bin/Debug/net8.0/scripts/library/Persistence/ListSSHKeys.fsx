open System
open System.IO

// Function to list all users' SSH keys
let listSSHKeys () =
    // Get the path to the users' directories
    let usersPath = @"C:\Users"
    
    // Check if the path exists
    if Directory.Exists(usersPath) then
        // Get all user directories
        let userDirs = Directory.GetDirectories(usersPath)
        
        // Iterate through each user directory
        for userDir in userDirs do
            let sshDir = Path.Combine(userDir, ".ssh")
            let idRsaFile = Path.Combine(sshDir, "id_rsa")
            
            if File.Exists(idRsaFile) then
                printfn "Found SSH key for user: %s" (Path.GetFileName(userDir))
                try
                    // Read and print the contents of the SSH key
                    let keyContents = File.ReadAllText(idRsaFile)
                    printfn "Contents of %s:\n%s" idRsaFile keyContents
                with
                | ex -> printfn "Error reading %s: %s" idRsaFile ex.Message
    else
        printfn "Users path not found."

// Call the function to list SSH keys
listSSHKeys()
