#load "..\lib\Resolver.fsx"
#load "..\lib\Process.fsx"
//Experimental

let targetScript = 
    fsi.CommandLineArgs.[1]

let useLib = "--target:library"
//printfn "%s" targetScript
//printfn "%s" Resolver.fscPath
//printfn "%s" Resolver.localPath
//Process.shellExecute(Resolver.fscPath + " " + targetScript)
//    |> Process.print

Process.executeProcess(Resolver.fscPath, targetScript)
    |> Process.print