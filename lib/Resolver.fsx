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

type FileType =
    | Fsx 
    | Batch 
    | Shell 
    | Powershell 
    //| All = 4   

type ScriptFile =
    {
        FileType: FileType;
        Name: string;
        Path: string
    }
type ScriptRole =
    | Script of ScriptFile
    | Library of ScriptFile

let private getModules path (allowedTypes: FileType[]) isRecursive =

    let getFilesIn path =
        let getFilesForExt path ext (fileType: FileType) =
            [|for file in (getFilesIn ext path) ->
                    {
                        FileType = fileType;
                        Name = Path.GetFileName(file);
                        Path = file
                    }
                |]
        let types = [|for t in allowedTypes ->
                        match t with
                        | FileType.Fsx -> getFilesForExt path "*.fsx" FileType.Fsx
                        | FileType.Batch -> getFilesForExt path "*.bat" FileType.Batch
                        | FileType.Shell -> getFilesForExt path "*.sh" FileType.Shell
                        | FileType.Powershell -> getFilesForExt path "*.ps1" FileType.Powershell
                        //| _ -> [||]
                        |] 
        types |> Array.collect (fun q -> q)
        //System.Linq.Enumerable.SelectMany(types, (fun s i -> s )
    let rec getModules path level =
        seq {   
                let shortDirName = Path.GetFileName(path)
                match shortDirName with
                //| t when t.Contains("lib")
                | "lib" -> 
                    for file in (getFilesIn path) do
                        match file with
                        | {Path = "_References.fsx"} -> ()       
                        | _ -> yield (Library(file), level)
                | "node_modules" -> ()
                | "bower_components"
                | "bin" -> 
                    for file in (getFilesIn path) do
                        match file with
                        | {Path = "_References.fsx"}  -> ()
                        | _ -> yield (Script(file), level)
                | _ -> 
                    for file in (getFilesIn path) do
                        match file with
                        | {Path = "_References.fsx"}  -> ()
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


let getGlobals =
    getModules globalBasePath

let getLocals =
    getModules localPath

let getScriptOptions getModulesFn =
    seq {
        for f in getModulesFn true do
            match f with
            | Script(path) -> yield path
            | _ -> ()
        }

let getClosestMatch name (allowedTypes : FileType[]) =
    let seq = getScriptOptions (getLocals allowedTypes)
                |> Seq.filter (fun q -> q.Path.Contains name)
    if not (Seq.isEmpty seq) then
        Some (Seq.head seq)
    else
        let seq = getScriptOptions (getGlobals allowedTypes)
                    |> Seq.filter (fun q -> q.Path.Contains name)
        if not (Seq.isEmpty seq) then
            Some (Seq.head seq)
        else
            None