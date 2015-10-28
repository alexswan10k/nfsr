#load "..\lib\_Nfsr.fsx"
if Args.has "-pr" then
    printfn "started %A" System.DateTime.Now
printfn "%s" ("list executing path " + Resolver.localPath)

let args = Args.getArgs()
let allowedTypes = _Nfsr.getAllowedTypes (args)

printfn "allowed types: "
allowedTypes |> Seq.iter (fun q -> printfn "%A" q)

let resolutionFileTypeFn =
    if Args.has("-l") || Args.has("--lib") then
        Resolver.getLibraries
    else
        Resolver.getScripts

for path in Resolver.searchPaths do
    printfn "scripts at %s" path.Path
    Resolver.getFiles path allowedTypes resolutionFileTypeFn
    |> Seq.map (fun q -> q.Path)
    |> Seq.map System.IO.Path.GetFileNameWithoutExtension
    |> Seq.iter (printfn "  %s")
if Args.has "-pr" then
    printfn "finished %A" System.DateTime.Now