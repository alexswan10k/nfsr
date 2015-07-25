#load "..\lib\Process.fsx"
#load "..\lib\Resolver.fsx"
#load "..\lib\Args.fsx"

open System.IO

let cmatch = 
    Resolver.getClosestMatch (fsi.CommandLineArgs.[1])

match cmatch with
| Some(m) ->    
                let join (arr: string[]) = System.String.Join(" ", arr)
                let fullPath = Resolver.fsiPath + " " + m
                let target = m + " " + join (fsi.CommandLineArgs |> Array.toSeq |> Seq.skip 2 |> Seq.toArray)

                let execute() = 
                    Process.executeProcess(Resolver.fsiPath, target) |> Process.print

                if Args.has "-h" || Args.has "--help" then
                    let helpfilePath = m.Replace(".fsx", "") + ".txt"
                    if System.IO.File.Exists(helpfilePath) then
                        File.ReadAllText(helpfilePath) |> printfn "%s"
                    else
                        execute()
                else
                    execute()

                

| none -> printfn "no match"