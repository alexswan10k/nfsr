open System.IO

let globalBasePath = """%appdata%\npm\node_modules\"""
let localPath = System.Environment.CurrentDirectory//__SOURCE_DIRECTORY__ 

let fsiPath = //"C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\Fsi.exe"
    """C:\Program Files (x86)\Microsoft SDKs\F#\4.0\Framework\v4.0\Fsi.exe"""

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

let getOptions() =
    getFilesAtThisLevel localPath "*.fsx"

let getClosestMatch name =
    let seq = getOptions()
                |> Seq.filter (fun q -> q.Contains name)
    if not (Seq.isEmpty seq) then
        Some (Seq.head seq)
    else
        None