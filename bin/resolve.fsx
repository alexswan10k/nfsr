#load "..\lib\Process.fsx"
#load "..\lib\_Nfsr.fsx"

open System.IO

let args = Args.getArgs()
let headParams = _Nfsr.getHeadParams (args)
let allowedTypes = _Nfsr.getAllowedTypes (headParams)

let cmatch = 
    Resolver.getClosestMatch (args.[headParams.Length + 1]) allowedTypes

match cmatch with
| Some(m) -> printfn "%s" cmatch.Value.Path
| None -> printfn "No match found"