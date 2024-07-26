open System
open System.IO

// Function to add an SSH public key for a specified user
let addSSHKey (username: string) (publicKey: string) =
    try
        // Define paths
        let usersPath = @"C:\Users"
        let userDir = Path.Combine(usersPath, username)
        let sshDir = Path.Combine(userDir, ".ssh")
        let authorizedKeysFile = Path.Combine(sshDir, "authorized_keys")
        
        // Check if the user directory exists
        if Directory.Exists(userDir) then
            // Check if the .ssh directory exists, create if it does not
            if not (Directory.Exists(sshDir)) then
                Directory.CreateDirectory(sshDir) |> ignore
            
            // Append the public key to the authorized_keys file
            File.AppendAllText(authorizedKeysFile, publicKey + Environment.NewLine)
            printfn "Added SSH key for user: %s" username
        else
            printfn "User directory not found: %s" userDir
    with
    | ex -> printfn "Error: %s" ex.Message

// Example usage
// Replace "username" with the actual username and "publicKey" with the actual SSH public key content
let username = "exampleuser"  // Change this to the target username
let publicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAABIwAAAQEArpJ9+3N8kDlF... your-public-key-content... user@example.com"  // Replace with actual public key

// Call the function to add the SSH key
addSSHKey username publicKey
