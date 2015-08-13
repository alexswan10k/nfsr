let getArgs() =
    #if INTERACTIVE
    let args = fsi.CommandLineArgs
    #else
    let args = System.Environment.GetCommandLineArgs()
    #endif
//    printfn "there are %A args.." args.Length
//    printfn "%A" args
    args

let hasFor arg (args:string[]) =
    [for a in args ->
        let firstPart = a.Split(':').[0] 
        if firstPart.Length > 0 && firstPart = arg then
            true
        else
            false
        ]
    |> List.exists id

let has (arg:string) =
    getArgs() |> hasFor arg

let contains (args: string[]) =
    let hasSubsequence (subSequence: seq<'a>) (mainSequence: seq<'a>) =     
        let rec traverseSeq seq = 
            if Seq.length seq > 0 then
                let subsequenceMatches = 
                    Seq.zip subSequence seq
                        |> Seq.forall(fun (a,b) -> a = b)

                if subsequenceMatches then
                    subsequenceMatches
                else
                    traverseSeq (seq |> Seq.toList |> List.tail |> List.toSeq)
            else
                false
        traverseSeq mainSequence
    getArgs() |> hasSubsequence (args |> Array.toSeq)

let getFor arg (args: string[]) =
    let firstMatch = 
        [for a in args ->
            match a.Split(':') with
            | [|argKey; argVal|] when argKey = arg -> Some(argVal)
            | _ -> None
            ]
        |> List.tryFind (fun q -> not (Option.isNone q))
    match firstMatch with
    | Some(x) -> x
    | None -> None

let get (arg: string) =
    getArgs() |> getFor arg 

let getOrDefault (arg: string) defaultRet =
    match get arg with
    | Some(argVal) -> argVal
    | None -> defaultRet