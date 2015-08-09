open System.IO
open System.Runtime.Serialization
open System.Reflection
open Microsoft.FSharp.Reflection
#load "Cache.fsx"
//#load "Ext/SharpXml.fsx"

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

//[<KnownType("GetKnownTypes")>]
//[<DataContract(Name="FileType", Namespace = "")>]
type FileType =
    | Fsx 
    | Batch 
    | Shell 
    | Powershell 
//    static member GetKnownTypes() =
//        Serialization.knownTypesForUnion<FileType>

    override x.ToString() =
        match x with
        | Fsx -> "fsx"
        | Batch -> "bat"
        | Shell -> "sh"
        | Powershell -> "ps1"


//[<DataContract(Name="ScriptFile", Namespace = "")>]
type ScriptFile =
    {
        FileType: FileType;
        Name: string;
        Path: string;
        Priority: int;
    }

//[<KnownType("GetKnownTypes")>]
//[<DataContract(Name="ScriptRole", Namespace = "")>]
type ScriptRole =
    | Script of ScriptFile
    | Library of ScriptFile
//    static member GetKnownTypes() =
//        Serialization.knownTypesForUnion<ScriptRole>

let private getModules path (allowedType: FileType) isRecursive =

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
        let fileGroups = [|for t in Seq.singleton allowedType ->
                            match t with
                            | FileType.Fsx -> getFilesForExt path "*.fsx" FileType.Fsx
                            | FileType.Batch -> getFilesForExt path "*.bat" FileType.Batch
                            | FileType.Shell -> getFilesForExt path "*.sh" FileType.Shell
                            | FileType.Powershell -> getFilesForExt path "*.ps1" FileType.Powershell
                            //| _ -> [||]
                            |] 
        fileGroups |> Array.collect (fun q -> q)
        //System.Linq.Enumerable.SelectMany(types, (fun s i -> s )
    let rec getModules path level =
        seq {   
                let shortDirName = Path.GetFileName(path)

                let categorizeByConvention (file : ScriptFile) =
                    if file.Name.Contains("lib.") || file.Name.StartsWith("lib") || file.Path.Contains("lib") then
                        Library(file)
                    else
                        Script(file)

                let isToBeExcluded (script:ScriptFile) =
                    script.Name = "_References.fsx" || script.Name = "_DynamicReferences.fsx" || script.Name = "_DynamicReferences.lock"

                match shortDirName with
                //| t when t.Contains("lib")
                | "lib" -> 
                    let score = level
                    for file in (getFilesIn path score) do
                        match file with
                        | f when isToBeExcluded f -> ()       
                        | _ -> yield (Library(file), score)
                | "node_modules" -> ()
                | "bower_components"
                | "bin" -> 
                    let score = level
                    for file in (getFilesIn path score) do
                        match file with
                        | f when isToBeExcluded f -> ()
                        | _ -> yield (categorizeByConvention(file), score)
                | _ -> 
                    let score = level + 100
                    for file in (getFilesIn path score) do
                        match file with
                        | f when isToBeExcluded f -> ()
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

let getScripts scriptRoles =
    seq {
        for f in scriptRoles do
            match f with
            | Script(file) -> yield file
            | _ -> ()
        }

let getLibraries scriptRoles =
    seq {
        for f in scriptRoles do
            match f with
            | Library(file) -> yield file
            | _ -> ()
        }

type SearchPath =
    {
        Path :string;
        AllowCache: bool
    }

let searchPaths = 
    [
        { Path = localPath; AllowCache = false};
        { Path = globalBasePath; AllowCache = true}
    ]
    |> List.toSeq

let getFiles (path:SearchPath) allowedTypes (getOptionsFn: seq<ScriptRole> -> seq<ScriptFile>) =
//        let getOrCreateCache cacheFileSuffix createFun = 
//
//            let hackySerialize (cache: Cache.Cache<ScriptRole[]>) =
//                let hackySerialize (items: ScriptRole[]) =
//                    let inner = 
//                        [|for sr in items ->
//                            SharpXml.XmlSerializer.SerializeToString(sr)|]
//                    SharpXml.XmlSerializer.SerializeToString(inner)
//                SharpXml.XmlSerializer.SerializeToString {Cache.Cache.Item = (hackySerialize cache.Item); Cache.Cache.Expiry = cache.Expiry}
//
//            let hackyDeserialize (txt: string) =
//                let hackyDeserialize (txt: string) =    
//                    let arr = SharpXml.XmlSerializer.DeserializeFromString<array<string>>(txt)
//                    [|for item in arr ->
//                        SharpXml.XmlSerializer.DeserializeFromString<ScriptRole>(item)|]
//                let cache = SharpXml.XmlSerializer.DeserializeFromString<Cache.Cache<string>>(txt)
//                {Cache.Cache.Item = (hackyDeserialize cache.Item); Cache.Cache.Expiry = cache.Expiry}
//
//            let cacheFile = globalBasePath + "\\nfsr\\nfsrcache"+cacheFileSuffix+".cache"
//            let cache = new Cache.CacheFileStore<array<ScriptRole>>(System.TimeSpan.FromDays(3.0), Store.FileStore(cacheFile, hackySerialize, hackyDeserialize))
//            if path.AllowCache then
//                let res = cache.GetOrCreate createFun
//                //printfn "using cache %s for %A" cacheFile res
//                res.Item
//            else
//                //printfn "not using cache"
//                createFun()

        //note this masks above fn. Temporary till serialization issue fixed
        let getOrCreateCache cacheFileSuffix createFun = createFun()

        let files = [|for t in allowedTypes ->
                        let cache = getOrCreateCache (t.ToString())
                                        (fun () -> 
                                            let files = (getModules path.Path t true) |> Seq.toArray
                                            //printfn "%A" files
                                            files)
                        cache
                        |]
                    |> Array.collect id
        let res = getOptionsFn (files |> Array.toSeq)
//        let cacheFile = globalBasePath + "\\nfsr\\nfsrtest.cache"
//        let cache = new Cache.CacheFileStore<array<ScriptFile>>(System.TimeSpan.FromDays(3.0), cacheFile)
//        cache.Overwrite (fun () -> (res |> Seq.toArray)) |> ignore
        res

let private getClosestMatch getOptionsFn name (allowedTypes : FileType[]) =

    let rec getClosestMatchRec (paths: seq<SearchPath>) =
        let path = Seq.head paths
        let options = getFiles path allowedTypes getOptionsFn
        let seq = options |> Seq.filter (fun q -> q.Name.Contains name)
        if not (Seq.isEmpty seq) then
            Some (Seq.head seq)
        else
            if Seq.length paths > 1 then
                getClosestMatchRec (Seq.skip 1 paths)
            else
                None

    getClosestMatchRec searchPaths

let getClosestScriptMatch =
    getClosestMatch getScripts

let getClosestLibraryMatch =
    getClosestMatch getLibraries


//let s1 = Serialization.serializeJson FileType.Fsx
//Serialization.deserializeJson<FileType>(s1)
//
//let s2 = Serialization.serializeJson {Name="file"; FileType=Fsx; Path="path";Priority=1}
//Serialization.deserializeJson<ScriptFile>(s2)
//let s3 = Serialization.serializeJson (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}))
//Serialization.deserializeJson<ScriptRole>(s3)
//
//
//let s4 =
//    Serialization.serializeJson [|
//        (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Library({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}))
//    |]
//Serialization.deserializeJson<array<ScriptRole>> s4
//
//
//let s4t = [|
//        (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Library({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}))
//    |]
//
//let string = sprintf "%A" s4t
//let quoted = <@@ string @@>

//let s5 = 
//       [| (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Library({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Script({Name="file"; FileType=Fsx; Path="path";Priority=1})) |]
//
//
//type Guest(id : int) =
//
//    let mutable firstName = Unchecked.defaultof<string>
//    let mutable lastName = Unchecked.defaultof<string>
//
//    member x.FirstName
//        with get() = firstName
//        and set v = firstName <- v
//    member x.LastName
//        with get() = lastName
//        and set v = lastName <- v
//    member x.Id
//        with get() = id

//SharpXml.XmlSerializer.SerializeToString(s5)
//
//let arrayList = System.Collections.ArrayList()
//arrayList.Add((Script({Name="file"; FileType=Fsx; Path="path";Priority=1})))
//arrayList.Add((Script({Name="file"; FileType=Fsx; Path="path";Priority=1})))
//arrayList.Add((Script({Name="file"; FileType=Fsx; Path="path";Priority=1})))
//
//let str = SharpXml.XmlSerializer.SerializeToString(arrayList)
//
//let newList = SharpXml.XmlSerializer.DeserializeFromString<System.Collections.Generic.List<ScriptRole>>(str)
//
//
//let singleThing = SharpXml.XmlSerializer.SerializeToString((Script({Name="file"; FileType=Fsx; Path="path";Priority=1})))
//let sres = SharpXml.XmlSerializer.DeserializeFromString<ScriptRole>(singleThing)
//
//
//let hackySerialize (items: ScriptRole[]) =
//    let inner = 
//        [|for sr in items ->
//            SharpXml.XmlSerializer.SerializeToString(sr)|]
//    SharpXml.XmlSerializer.SerializeToString(inner)
//
//let hackyDeserialize (txt: string) =    
//    let arr = SharpXml.XmlSerializer.DeserializeFromString<array<string>>(txt)
//    [for item in arr ->
//        SharpXml.XmlSerializer.DeserializeFromString<ScriptRole>(item)]
//
//let someText = hackySerialize s5
//let res = hackyDeserialize someText
//Microsoft.FSharp.Compiler.Interactive.