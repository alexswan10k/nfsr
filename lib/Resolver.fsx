open System.IO

let globalBasePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + """\npm\node_modules\"""
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

//let getDirectoriesIn dir pattern =
//    Directory.EnumerateDirectories(dir)

type FsxType =
    | Script of string
    | Library of string

let private getModules path isRecursive =
    let rec getModules path level =
        seq {   
                let shortDirName = Path.GetFileName(path)
                match shortDirName with
                //| t when t.Contains("lib")
                | "lib" -> 
                    for file in (getFsxFilesIn path) do
                        match Path.GetFileName(file) with
                        | "_References.fsx" -> ()
                        | _ -> yield (Library(file), level)
                | "node-modules" -> ()
                | "bin" -> 
                    for file in (getFsxFilesIn path) do
                        match Path.GetFileName(file) with
                        | "_References.fsx" -> ()
                        | _ -> yield (Script(file), level)
                | _ -> 
                    for file in (getFsxFilesIn path) do
                        match Path.GetFileName(file) with
                        | "_References.fsx" -> ()
                        | _ -> yield (Script(file), level + 100) //penalise scripts not conforming to convention. These should resolve after scripts in bin
                for d in Directory.EnumerateDirectories(path) do
                    yield! getModules d (level + 1)
            }
    let modules = getModules path 0
                    |> Seq.sortBy(fun (file, level) -> level)
    let sortedModules =
        if isRecursive then
            modules
        else
            if Seq.length modules > 0 then
                let (_, lowestLevel) = Seq.head modules
                modules |> Seq.filter (fun (file, level) -> level = lowestLevel)
            else
                modules
    sortedModules |> Seq.map(fun (file, level) -> file)


let getGlobals isRecursive =
    getModules globalBasePath isRecursive

let getLocals isRecursive =
    getModules localPath isRecursive

let getScriptOptions getModulesFn =
    seq {
        for f in getModulesFn true do
            match f with
            | Script(path) -> yield path
            | _ -> ()
        }

let getClosestMatch name =
    let seq = getScriptOptions getLocals
                |> Seq.filter (fun q -> q.Contains name)
    if not (Seq.isEmpty seq) then
        Some (Seq.head seq)
    else
        let seq = getScriptOptions getGlobals
                    |> Seq.filter (fun q -> q.Contains name)
        if not (Seq.isEmpty seq) then
            Some (Seq.head seq)
        else
            None