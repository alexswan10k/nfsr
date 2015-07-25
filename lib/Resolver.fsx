open System.IO

let globalBasePath = """%appdata%\npm\node_modules\"""
let localPath = System.Environment.CurrentDirectory//__SOURCE_DIRECTORY__ 

let fsharpPath = 
    let px86 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86)
    let directoriesToCheck = [
        px86 + """\Microsoft SDKs\F#\4.0\Framework\v4.0"""
        px86 + """\Microsoft SDKs\F#\3.1\Framework\v4.0"""
        px86 + """\Microsoft SDKs\F#\3.0\Framework\v4.0"""
    ]
    directoriesToCheck |> List.find (fun q -> Directory.Exists(q))
printfn "%s" fsharpPath    
let fsiPath =
    fsharpPath + """\Fsi.exe"""

let fscPath = 
    fsharpPath + """\Fsc.exe"""

let rec getAllFiles dir pattern =
    seq { yield! Directory.EnumerateFiles(dir, pattern)
          for d in Directory.EnumerateDirectories(dir) do
              yield! getAllFiles d pattern }

let getFilesIn pattern dir =
    Directory.EnumerateFiles(dir, pattern)
let getFsxFilesIn =
    getFilesIn "*.fsx"

let getDirectoriesIn dir pattern =
    Directory.EnumerateDirectories(dir)

type FsxType =
    | Script of string
    | Library of string

let rec getModules path =
    seq {   //this fn needs work. Needs to be aware of ancestor lib folder etc
            let shortDirName = Path.GetFileName(path)
            match shortDirName with
            //| t when t.Contains("lib")
            | "lib" -> 
                for file in (getFsxFilesIn path) do
                    match Path.GetFileName(file) with
                    | "_References.fsx" -> ()
                    | _ -> yield Library(file)
            | "node-modules" -> ()
            //| "bin" -> 
            | _ -> 
                for file in (getFsxFilesIn path) do
                    match Path.GetFileName(file) with
                    | "_References.fsx" -> ()
                    | _ -> yield Script(file)
            for d in Directory.EnumerateDirectories(path) do
                yield! getModules d
        }

let getGlobals() =
    getModules globalBasePath

let getLocals() =
    getModules localPath

let getOptions() =
    seq {
        for f in getLocals() do
            //printfn "%s" (f.ToString())
            match f with
            | Script(path) -> yield path
            | _ -> ()
        }
    //getFsxFilesIn localPath

let getClosestMatch name =
    let seq = getOptions()
                |> Seq.filter (fun q -> q.Contains name)
    if not (Seq.isEmpty seq) then
        Some (Seq.head seq)
    else
        None