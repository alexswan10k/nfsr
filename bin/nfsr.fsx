#load "..\lib\Process.fsx"
open System.IO

let globalBasePath = """%appdata%\npm\node_modules\"""
let localPath = __SOURCE_DIRECTORY__ 

let fsiPath = "C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\Fsi.exe"

//printfn "Test"

let rec getAllFiles dir pattern =
    seq { yield! Directory.EnumerateFiles(dir, pattern)
          for d in Directory.EnumerateDirectories(dir) do
              yield! getAllFiles d pattern }

let getFilesAtThisLevel dir pattern =
    Directory.EnumerateFiles(dir, pattern)

let getScripts modulePath =
    let finalPath = Path.Combine(modulePath, "bin")
    ()
let getLibs modulePath =
    ()

let getClosestMatch name =
    let seq = getFilesAtThisLevel localPath "*.fsx"
                |> Seq.filter (fun q -> q.Contains name)
    if not (Seq.isEmpty seq) then
        Some (Seq.head seq)
    else
        None

let cmatch = 
    getClosestMatch (fsi.CommandLineArgs.[1])

match cmatch with
| Some(m) ->    printfn "%s" m
                //let join (arr: string[]) = System.String.Join(" ", arr |> Array.toSeq |> Seq.skip 2)
                //Process.executeProcess(fsiPath, (join fsi.CommandLineArgs)) |> ignore
                Process.executeProcess(fsiPath, m) |> ignore
| none -> printfn "no match"
