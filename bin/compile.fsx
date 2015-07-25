#load "..\lib\Resolver.fsx"
#load "..\lib\Process.fsx"
//Experimental
//see http://softwareblog.morlok.net/2008/07/05/f-compiler-fscexe-command-line-options/

let private join (arr: string[]) = System.String.Join(" ", arr)

let private args = fsi.CommandLineArgs 
                    |> Array.toSeq |> Seq.skip 1 |> Seq.toArray
                    |> join

//let useLib = "--target:library"
//printfn "%s" targetScript
//printfn "%s" Resolver.fscPath
//printfn "%s" Resolver.localPath
//Process.shellExecute(Resolver.fscPath + " " + targetScript)
//    |> Process.print

Process.executeProcess(Resolver.fscPath, args)
    |> Process.print