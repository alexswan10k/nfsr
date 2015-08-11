#load "..\lib\Process.fsx"
#load "..\lib\_Nfsr.fsx"

open System.IO

let args = Args.getArgs()
let allowedTypes = _Nfsr.getAllowedTypes (args)

printfn "allowed types: "
allowedTypes |> Seq.iter (fun q -> printfn "%A" q)

let cmatch = 
    if Args.has("-l") || Args.has("--lib") then
        Resolver.getClosestLibraryMatch (args.[1]) allowedTypes
    else
        Resolver.getClosestScriptMatch (args.[1]) allowedTypes

match cmatch with
| Some(m) -> printfn "%s" cmatch.Value.Path
| None -> printfn "No match found"