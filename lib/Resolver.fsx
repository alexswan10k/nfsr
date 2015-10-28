#r "System.Xml.Linq.dll"
open System.IO
open System.Runtime.Serialization
open System.Reflection
open System.Xml.Linq
open Microsoft.FSharp.Reflection
#load "Cache.fsx"

let globalBasePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + """\npm\node_modules\"""
let localPath = System.Environment.CurrentDirectory//__SOURCE_DIRECTORY__ 
let private configPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile)

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

//let rec private getAllFiles dir pattern =
//    seq { yield! Directory.EnumerateFiles(dir, pattern)
//          for d in Directory.EnumerateDirectories(dir) do
//              yield! getAllFiles d pattern }

let getFilesIn pattern dir =
    try Directory.EnumerateFiles(dir, pattern)  //hack for long paths
    with _ -> System.Linq.Enumerable.Empty<string>()

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
    | Dll
//    static member GetKnownTypes() =
//        Serialization.knownTypesForUnion<FileType>
    static member fromString = function
        | "fsx" -> Fsx
        | "bat" -> Batch
        | "sh" -> Shell
        | "ps1" -> Powershell;
        | "dll" -> Dll;
        | _ -> failwith "unknown"
    override x.ToString() =
        match x with
        | Fsx -> "fsx"
        | Batch -> "bat"
        | Shell -> "sh"
        | Powershell -> "ps1"
        | Dll -> "dll"


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
                    let fileName =
                        try Path.GetFileName(file) //hack for long paths
                        with _ -> "" 
                    
                    {
                        FileType = fileType;
                        Name = fileName;
                        Path = file;
                        Priority = priority
                    }
                |] |> Array.filter (fun q -> q.Name.Length > 0)
        let fileGroups = [|for t in Seq.singleton allowedType ->
                            match t with
                            | FileType.Fsx -> getFilesForExt path "*.fsx" FileType.Fsx
                            | FileType.Batch -> getFilesForExt path "*.bat" FileType.Batch
                            | FileType.Shell -> getFilesForExt path "*.sh" FileType.Shell
                            | FileType.Powershell -> getFilesForExt path "*.ps1" FileType.Powershell
                            | FileType.Dll -> getFilesForExt path "*.dll" FileType.Dll
                            //| _ -> [||]
                            |] 
        fileGroups |> Array.collect (fun q -> q)
        //System.Linq.Enumerable.SelectMany(types, (fun s i -> s )
    let rec getModules path level =
        seq {   
                let shortDirName = Path.GetFileName(path)

                let categorizeByConvention (file : ScriptFile) =
                    if file.Name.Contains("lib.") || file.Name.StartsWith("lib") || file.Path.Contains("lib") || file.FileType = FileType.Dll then
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

                let dirs = 
                    try Directory.EnumerateDirectories(path) //hack for long paths
                    with _ -> System.Linq.Enumerable.Empty<string>()
                for d in dirs do
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
    let serialize (list: list<SearchPath>) =
        let XElement(name, content) = new XElement(XName.Get name, Seq.toArray content)
        let XAttribute(name, value) = new XAttribute(XName.Get name, value)
        let toObjArr arr = System.Linq.Enumerable.Cast<obj>(arr)
        XElement("Config",
            [for path in list ->
                XElement("SearchPath",
                    [
                        XElement("Path", [path.Path])
                        XElement("AllowCache", [path.AllowCache])
                    ]
                    )
                ] |> toObjArr
            ).ToString()

    let deserialize (s : string) =
        let xn name = XName.Get name
        let xd = XDocument.Parse(s)
        [
            for p in xd.Element(xn "Config").Elements(xn "SearchPath") ->
                {Path = p.Element(xn "Path").Value; AllowCache = bool.Parse(p.Element(xn "AllowCache").Value)}
        ]

    let store = Store.FileStore<list<SearchPath>>(configPath + """\nfsr.config""",serialize, deserialize)
    
    let storePaths = store.GetOrCreate(fun() -> [{ Path = globalBasePath; AllowCache = true}])
    let allPaths = { Path = localPath; AllowCache = false}::storePaths
    allPaths |> List.toSeq

let getFiles (path:SearchPath) allowedTypes (getOptionsFn: seq<ScriptRole> -> seq<ScriptFile>) =
        let getOrCreateCache cacheFileSuffix createFun = 
            let hackySerialize (cache: Cache.Cache<ScriptRole[]>) =
                let XElement(name, content) = new XElement(XName.Get name, Seq.toArray content)
                let XAttribute(name, value) = new XAttribute(XName.Get name, value)
                let toObjArr arr = System.Linq.Enumerable.Cast<obj>(arr)
                let xd = XElement("Cache",
                            [
                            XElement("Expiry", [cache.Expiry])//,
                            XElement("Item", 
                                let dumpFile (sf: ScriptFile) =
                                    [
                                        XElement("FileType", [sf.FileType])
                                        XElement("Name", [sf.Name])
                                        XElement("Path", [sf.Path])
                                        XElement("Priority", [sf.Priority])
                                    ] |> toObjArr
                                [for role in cache.Item ->
                                    match role with
                                    | Script(t) -> XElement("Script", dumpFile t)
                                    | Library(t) -> XElement("Library", dumpFile t)
                                    ] |> toObjArr)
                            ])
                xd.ToString()

            let hackyDeserialize (txt: string) = 
                let xn name = XName.Get name
                let xd = XDocument.Parse(txt)
                {
                    Cache.Cache.Item = 
                        [|
                            for xe in xd.Element(xn "Cache").Element(xn "Item").Elements() ->
                                let buildFile (xe: XElement) =
                                    {
                                        FileType = FileType.fromString (xe.Element(xn "FileType").Value);
                                        Name = (xe.Element(xn "Name").Value);
                                        Path = (xe.Element(xn "Path").Value);
                                        Priority = System.Int32.Parse((xe.Element(xn "Priority").Value));
                                    }

                                match xe.Name.LocalName with
                                | "Script" ->  
                                    Script(buildFile xe)
                                | "Library" ->  
                                    Library(buildFile xe)
                                | _ -> failwith "unknown type"
                        |]; 
                    Cache.Expiry = System.DateTime.Parse(xd.Element(xn "Cache").Element(xn "Expiry").Value)
                }

            let cacheFile = globalBasePath + "\\nfsr\\"+cacheFileSuffix + path.GetHashCode().ToString() + ".cache"
            let cache = new Cache.CacheFileStore<array<ScriptRole>>(System.TimeSpan.FromDays(3.0), Store.FileStore(cacheFile, hackySerialize, hackyDeserialize))
            if path.AllowCache then
                let res = cache.GetOrCreate createFun
                //printfn "using cache %s for %A" cacheFile res
                res.Item
            else
                //printfn "not using cache"
                createFun()

        //note this masks above fn. Temporary till serialization issue fixed
        //let getOrCreateCache cacheFileSuffix createFun = createFun()

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
        let seq = options |> Seq.filter (fun q -> Path.GetFileNameWithoutExtension(q.Name) = name)
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



//let s5 = 
//       [| (Script({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Library({Name="file"; FileType=Fsx; Path="path";Priority=1}));
//        (Script({Name="file"; FileType=Fsx; Path="path";Priority=1})) |]
//
