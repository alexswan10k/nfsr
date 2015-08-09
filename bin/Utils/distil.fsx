#load "..\..\lib\Args.fsx"
#r "System.Core.dll"
#r "System.Xml.Linq.dll"
open System.Xml.Linq

open System.IO

let private rootPath path = 
    if Path.IsPathRooted(path) then
        path
    else
        Path.Combine(System.Environment.CurrentDirectory, path)

let rec private getAllFilesMatching path pattern = 
    seq {
        for dir in Directory.EnumerateDirectories(path) do
            yield! getAllFilesMatching (dir) pattern
        for file in Directory.EnumerateFiles(path, pattern) do
            yield file
        }

let targetPath = 
    Args.getOrDefault "--target-path" System.Environment.CurrentDirectory
    |> rootPath

let outputFile =
    Args.getOrDefault "--output" "Output"

if File.Exists(outputFile + ".fs") then
    printfn "deleting existing %s.fs" outputFile
    File.Delete(outputFile+ ".fs")

if File.Exists(outputFile + ".fsx") then
    printfn "deleting existing %s.fsx" outputFile
    File.Delete(outputFile+ ".fsx")

let targetProject = 
    match Args.get "--target-project" with
    | Some(path) -> 
        let rootedPath = rootPath path
        if not (File.Exists(rootedPath)) then
            failwith "Project does not appear to exist matching that path"
        Some(rootPath path)
    | None -> 
        //Otherwise go find the first project file
        let sRes = getAllFilesMatching targetPath "*.fsproj"
        if not (Seq.isEmpty sRes) then
            Some(Seq.head sRes)
        else
            None

let skipAssemblyInfo = true


//type FileType = Fs of string | Fsx of string

type ProjectResult =
    {Files : seq<string>;
    References: seq<string>;
    DependentProjects: seq<ProjectResult>}

let rec private getAllProjectStuff (projectPath: string) =
    let projectDir = Path.GetDirectoryName(projectPath)
    let xn s = XName.Get(s, "http://schemas.microsoft.com/developer/msbuild/2003")
    let doc = XDocument.Load(projectPath)
    printfn "loaded %s" projectPath
    let itemGroups = doc.Element(xn "Project").Elements(xn "ItemGroup")
    
    let dependentProjects = 
        seq {
            for group in itemGroups do
                yield! group.Elements(xn "ProjectReference").Attributes(XName.Get("Include"))
            }
        |> Seq.map(fun a -> Path.Combine(projectDir, a.Value))
        |> Seq.map(fun ares -> getAllProjectStuff ares) //rec
    //load dependent project first
    let references = 
        seq {
            for group in itemGroups do
                yield! group.Elements(xn "Reference").Attributes(XName.Get("Include"))
            }
        |> Seq.map(fun a -> a.Value)

    let fsFiles = 
        seq {
            for group in itemGroups do
                yield! group.Elements(xn "Compile").Attributes(XName.Get("Include"))
            }
        |> Seq.filter(fun q -> 
            if skipAssemblyInfo then
                not (q.Value = "AssemblyInfo.fs")
            else true)
        |> Seq.map(fun a -> Path.Combine(projectDir, a.Value))

    {Files= fsFiles; References = references; DependentProjects = dependentProjects}

match targetProject with
| Some(path) -> 
    let pres = getAllProjectStuff path

    let indent line =
        "  " + line

    let processFile (lines: string[]) =
        let res, accVal = lines 
                            |> Array.mapFold 
                            (fun acc line -> 
                                if System.Text.RegularExpressions.Regex.IsMatch(line, "namespace\s[\w\.]+$") then
                                    (line + " =", true)
                                else
                                    if acc then //all lines proceeding a namespace arg get indented
                                        (indent line, acc)
                                    else
                                        (line, acc)
                                ) false
        res

    let createMasterNamespace (lines: seq<string>) =
        seq {
                yield "namespace "+ outputFile
                yield! lines |> Seq.map(indent)
            }

    let prefixStamp (lines: seq<string>) =
        seq {
            yield "//Generated from "+ Path.GetFileName(path) + " using nfsr distil"
            yield! lines
        }

    let rec generateFs (pres: ProjectResult) = 
        seq{ 
            for dp in pres.DependentProjects do
                yield! generateFs dp

            for path in pres.Files do
                yield "//From " + Path.GetFileName(path)
                let lines = File.ReadAllLines(path)
                yield! lines
                //yield! processFile lines
                yield "//End of " + Path.GetFileName(path)
            }

    let generateFsx (pres: ProjectResult) =
        let rec generateFsx' (pres: ProjectResult) = 
            seq{ 
                for dp in pres.DependentProjects do
                    yield! generateFsx' dp

                for path in pres.References do
                    yield "#r \"\"\""+path+"\"\"\""
                }
        let res = generateFsx' pres 
                    |> Seq.distinct
        Seq.append res (Seq.singleton ("#load \"\"\""+ outputFile + ".fs\"\"\""))

    printfn "generating %s" (outputFile + ".fs")
    File.WriteAllLines(outputFile + ".fs", generateFs pres |> prefixStamp)
    printfn "generating %s" (outputFile + ".fsx")
    File.WriteAllLines(outputFile + ".fsx", generateFsx pres |> prefixStamp)


    //getAllProjectFiles path |> printfn "%A"
| None -> ()


//
//let mergeFiles (filePaths : FileType[]) =
//    ()
//
//getAllFiles targetPath