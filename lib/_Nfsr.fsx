#load "Resolver.fsx"
#load "Args.fsx"

let private split (args: string[])=
    let isDashedArg (arg:string) =
        arg.StartsWith("-")

    let idxs = args |> Array.mapi (fun idx arg -> 
                        if not (isDashedArg arg) then Some(idx) else None)
                    |> Array.append [|Some(-1); Some(args.Length)|]
                    |> Array.filter (fun q -> not (Option.isNone q))
                    |> Array.map (fun q -> Option.get q)
                    |> Array.sort
                    |> Array.toSeq |> Seq.pairwise

    [|for (a, b) in idxs ->
//        printfn "(a, b) = (%s, %s)" (a.ToString()) (b.ToString())
//        printfn "splitting at (%s, %s)" (a.ToString()) ((b - a).ToString())
        args |> Seq.skip (a+1)
            |> Seq.take (b - (a+1)) |> Seq.toArray
        |]
        //Dont think we can filter here as this causes other problems
//    |> Array.filter (fun q -> q.Length > 0)

//split [|"-r"; "-f"; "tim-script"; "-x" ; "-y:14"; "rat"; "-z:gggg"; "n:fsrR"; "je"; "innit"; "-paramLast"|]

let getHeadParams (args: string[]) = 
    split args
        |> Array.toSeq |> Seq.head

let fn = Args.hasFor
let g = Resolver.FileType.Fsx

let getAllowedTypes args =
    let outSeq = seq {
            let has arg = Args.hasFor arg args
            if has "-a" then
                yield Resolver.FileType.Fsx
                yield Resolver.FileType.Batch
                yield Resolver.FileType.Powershell
                yield Resolver.FileType.Shell
            else
                if has "-f" then
                    yield Resolver.FileType.Fsx
                if has "-b" then
                    yield Resolver.FileType.Batch
                if has "-p" then
                    yield Resolver.FileType.Powershell
                if has "-s" then
                    yield Resolver.FileType.Shell
        }
    let res = 
        if outSeq |> Seq.length = 0 then
            Seq.singleton Resolver.FileType.Fsx
        else
            outSeq
        |> Seq.toArray
    //res |> Array.iter (fun q -> printfn "%s" (q.ToString()))
    res