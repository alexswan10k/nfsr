#load "..\lib\Process.fsx"
#load "..\lib\Resolver.fsx"

let cmatch = 
    Resolver.getClosestMatch (fsi.CommandLineArgs.[1])

printfn "%s" ("nfsr executing path " + Resolver.localPath)

match cmatch with
| Some(m) ->    
                let join (arr: string[]) = System.String.Join(" ", arr)
                let fullPath = Resolver.fsiPath + " " + m
                let target = m + " " + join (fsi.CommandLineArgs |> Array.toSeq |> Seq.skip 2 |> Seq.toArray)
                printfn "%s" ("fsi " + target)
                Process.executeProcess(Resolver.fsiPath, target) |> Process.print

| none -> printfn "no match"