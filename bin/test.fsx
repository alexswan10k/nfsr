printfn "Test"
printfn "args: "

let join (arr: string[]) = System.String.Join(" ", arr)
let args = (fsi.CommandLineArgs |> Array.toSeq |> Seq.skip 1 |> Seq.toArray)
printfn "%s" (join args)
printfn "end"