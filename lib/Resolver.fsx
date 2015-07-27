open System.IO
open System.Runtime.Serialization
open System.Reflection
open FSharp.Reflection
#load "Cache.fsx"

let globalBasePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + """\npm\node_modules\"""
let localPath = System.Environment.CurrentDirectory//__SOURCE_DIRECTORY__ 

let private fsharpPath = 
    let px86 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86)
    let directoriesToCheck = [
        px86 + """\Microsoft SDKs\F#\4.0\Framework\v4.0"""
        px86 + """\Microsoft SDKs\F#\3.1\Framework\v4.0"""
        px86 + """\Microsoft SDKs\F#\3.0\Framework\v4.0"""
    ]
    directoriesToCheck |> List.find (fun q -> Directory.Exists(q))

let private powershellPathN = 
    let windowsFldr = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows)
    let directoriesToCheck = [
        windowsFldr + """\System32\WindowsPowerShell\v1.0"""
    ]
    directoriesToCheck |> List.find (fun q -> Directory.Exists(q))

let fsiPath =
    fsharpPath + """\Fsi.exe"""

let fscPath = 
    fsharpPath + """\Fsc.exe"""

let powershellPath = 
    powershellPathN + """\Powershell.exe"""

let rec private getAllFiles dir pattern =
    seq { yield! Directory.EnumerateFiles(dir, pattern)
          for d in Directory.EnumerateDirectories(dir) do
              yield! getAllFiles d pattern }

let getFilesIn pattern dir =
    Directory.EnumerateFiles(dir, pattern)
let getFsxFilesIn =
    getFilesIn "*.fsx"

//let getDirectoriesIn dir pattern =
//    Directory.EnumerateDirectories(dir)

[<KnownType("GetKnownTypes")>]
type FileType =
    | Fsx 
    | Batch 
    | Shell 
    | Powershell 
    static member GetKnownTypes =
        Serialization.getKnownTypes<FileType>

type ScriptFile =
    {
        FileType: FileType;
        Name: string;
        Path: string;
        Priority: int;
    }

[<KnownType("GetKnownTypes")>]
type ScriptRole =
    | Script of ScriptFile
    | Library of ScriptFile
    static member GetKnownTypes =
        Serialization.getKnownTypes<ScriptRole>

let private globalCache = new Cache.CacheFileStore<ScriptRole[]>(System.TimeSpan.FromDays(3.0), globalBasePath + "\\nfsr\\nfsr.cache")

let private getModules path (allowedTypes: FileType[]) isRecursive =

    let getFilesIn path priority =
        let getFilesForExt path ext (fileType: FileType) =
            [|for file in (getFilesIn ext path) ->
                    {
                        FileType = fileType;
                        Name = Path.GetFileName(file);
                        Path = file;
                        Priority = priority
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

                let categorizeByConvention (file : ScriptFile) =
                    if file.Name.Contains("lib.") || file.Name.StartsWith("lib") || file.Path.Contains("lib") then
                        Library(file)
                    else
                        Script(file)

                match shortDirName with
                //| t when t.Contains("lib")
                | "lib" -> 
                    let score = level
                    for file in (getFilesIn path score) do
                        match file with
                        | f when f.Name = "_References.fsx" -> ()       
                        | _ -> yield (Library(file), score)
                | "node_modules" -> ()
                | "bower_components"
                | "bin" -> 
                    let score = level
                    for file in (getFilesIn path score) do
                        match file with
                        | f when f.Name = "_References.fsx"   -> ()
                        | _ -> yield (categorizeByConvention(file), score)
                | _ -> 
                    let score = level + 100
                    for file in (getFilesIn path score) do
                        match file with
                        | f when f.Name = "_References.fsx"   -> ()
                        | _ -> yield (categorizeByConvention(file), score) //penalise scripts not conforming to convention. These should resolve after scripts in bin
                if path.Length < 240 then
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
            | Script(file) -> yield file
            | _ -> ()
        }

let getLibOptions getModulesFn =
    seq {
        for f in getModulesFn true do
            match f with
            | Library(file) -> yield file
            | _ -> ()
        }

let private getClosestMatch getScriptOptions name (allowedTypes : FileType[]) =
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

let getClosestScriptMatch =
    getClosestMatch getScriptOptions

let getClosestLibraryMatch =
    getClosestMatch getLibOptions