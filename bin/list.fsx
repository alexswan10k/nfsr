#load "..\lib\Resolver.fsx"

printfn "%s" ("list executing path " + Resolver.localPath)

printfn "local scripts:"
Resolver.getScriptOptions Resolver.getLocals 
    |> Seq.map System.IO.Path.GetFileNameWithoutExtension
    |> Seq.iter (printfn "%s")

printfn "global scripts:"
Resolver.getScriptOptions Resolver.getGlobals 
    |> Seq.map System.IO.Path.GetFileNameWithoutExtension
    |> Seq.iter (printfn "%s")