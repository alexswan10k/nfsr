#load "..\lib\_Nfsr.fsx"
#load "..\lib\ListUtils.fsx"

open System.IO
open ListUtils
//add library > Searches global and local for library.
//restore > restores je

let args = Args.getArgs() |> Array.toSeq |> Seq.skip 1 |> Seq.toArray
let allowedTypes = _Nfsr.getAllowedTypes (args)
let lockPath = Resolver.localPath + "\\_DynamicReferences.lock"
let activePath = Resolver.localPath + "\\_DynamicReferences.fsx"

let updateRefs (arr: array<string>) =
    let fetchReferences (arr: array<string>) =
        [|for r in arr do
            //updateReferenceFor r
            match Resolver.getClosestLibraryMatch (r) allowedTypes with
            | Some(file) -> 
                yield file.Path
            | None -> ()
            |]
    let res = fetchReferences arr 
                |> Array.map (fun q -> "#load \"\"\"" + q + "\"\"\"")
    printfn "updating references file from lock file"
    File.WriteAllLines(activePath, res)
    printfn "writing %s %A" lockPath res
    ()

let lockData = 
    if File.Exists lockPath then
        File.ReadAllLines(lockPath)
    else [||]

match args |> Array.toList with
| ElementsAfter ["add"] (libName::_) -> 
    if (Resolver.getClosestLibraryMatch (libName) allowedTypes).IsSome then
        let dataUpdated = 
            if not (lockData |> Array.exists (fun q -> q = libName)) then
                let newArr = lockData |> Array.append [|libName|]
                File.WriteAllLines(lockPath, newArr)
                printfn "Resolved %s to %s %A" libName lockPath newArr
                newArr
            else lockData
        updateRefs dataUpdated
    else
        printfn "Could not find %s" libName

| ElementsAfter ["remove" ] (libName::_) -> 
    if lockData |> Array.exists (fun q -> q = libName) then
        let dataUpdated = 
            let newArr = lockData |> Array.filter (fun q -> not(q = libName))
            File.WriteAllLines(lockPath, newArr)
            printfn "writing %s %A" lockPath newArr
            newArr
        updateRefs dataUpdated
    else
        printfn "%s not found" libName

| ElementsAfter ["refresh"] _ -> 
    updateRefs lockData
    ()
| _ -> printfn "no action taken"