let getArgs() =
    let args = fsi.CommandLineArgs
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

let getFor arg (args: string[]) =
    [for a in args ->
        match a.Split(':') with
        | [|argKey; argVal|] when argKey = arg -> Some(argVal)
        | _ -> None
        ]
    |> List.tryFind (fun q -> not (Option.isNone q))
    |> Option.get

let get (arg: string) =
    getArgs() |> getFor arg 