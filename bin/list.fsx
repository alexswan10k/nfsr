#load "..\lib\_Nfsr.fsx"

printfn "%s" ("list executing path " + Resolver.localPath)

let args = Args.getArgs()
let allowedTypes = _Nfsr.getAllowedTypes (args)

printfn "allowed types: "
allowedTypes |> Seq.iter (fun q -> printfn "%A" q)

printfn "local scripts:"
Resolver.getScriptOptions (Resolver.getLocals allowedTypes)
    |> Seq.map (fun q -> q.Path)
    |> Seq.map System.IO.Path.GetFileNameWithoutExtension
    |> Seq.iter (printfn "%s")

printfn ""
printfn "global scripts:"
Resolver.getScriptOptions (Resolver.getGlobals allowedTypes)
    |> Seq.map (fun q -> q.Path)
    |> Seq.map System.IO.Path.GetFileNameWithoutExtension
    |> Seq.iter (printfn "%s")