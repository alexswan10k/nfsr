#load "..\lib\_Nfsr.fsx"

open System.IO
//add library > Searches global and local for library.
//restore > restores je

let args = Args.getArgs() |> Array.toSeq |> Seq.skip 1 |> Seq.toArray
let allowedTypes = _Nfsr.getAllowedTypes (args)
let path = Resolver.localPath + "\\_DynamicReferences.lock"
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
    File.WriteAllLines(activePath, res)
    printfn "writing %s %A" path res
    ()

let data = 
    if File.Exists path then
        File.ReadAllLines(path)
    else [||]

match args with
| [|"add"; libName; _|] -> 

    let dataUpdated = 
        if not (data |> Array.exists (fun q -> q = libName)) then
            let newArr = data |> Array.append [|libName|]
            File.WriteAllLines(path, newArr)
            printfn "writing %s %A" path newArr
            newArr
        else data
    updateRefs dataUpdated

| [|"remove"; libName; _|] -> 
    let dataUpdated = 
        let newArr = data |> Array.filter (fun q -> not(q = libName))
        File.WriteAllLines(path, newArr)
        printfn "writing %s %A" path newArr
        newArr
    updateRefs dataUpdated

| [|"refresh"; _|] -> 
    updateRefs data
    ()
| _ -> printfn "no action taken"