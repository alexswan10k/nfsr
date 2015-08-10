#load "..\lib\Args.fsx"

open System.IO
//add library > Searches global and local for library.
//restore > restores je

let args = Args.getArgs() |> Array.toSeq |> Seq.skip 1 |> Seq.toArray

let globalCacheDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + """\npm\node_modules\nfsr"""

match args with
| [|"clean"|] -> 
    for file in Directory.GetFiles(globalCacheDir, "*.cache") do
        printfn "removing %s" file
        File.Delete(file)
| _ -> ()