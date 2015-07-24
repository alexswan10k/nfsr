#load "..\lib\Resolver.fsx"

Resolver.getOptions() 
    |> Seq.map System.IO.Path.GetFileNameWithoutExtension
    |> Seq.iter (printfn "%s")