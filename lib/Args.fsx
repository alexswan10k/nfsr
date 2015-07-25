let has (hasArg:string) =
    let args = fsi.CommandLineArgs
    [for arg in args ->
        if arg = hasArg then
            true
        else
            false
        ]
    |> List.exists id

//other useful functions for getting out arguments from fsi.CommandLineArgs