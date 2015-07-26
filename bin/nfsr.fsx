#load "..\lib\Process.fsx"
#load "..\lib\_Nfsr.fsx"

open System.IO

let args = Args.getArgs()
let headParams = _Nfsr.getHeadParams (args)
let allowedTypes = _Nfsr.getAllowedTypes (headParams)

let cmatch = 
    Resolver.getClosestMatch (args.[headParams.Length + 1]) allowedTypes

match cmatch with
| Some(m) ->    
                let join (arr: string[]) = System.String.Join(" ", arr)
                let fullPath = Resolver.fsiPath + " " + m.Path
                let target = m.Path + " " + join (args |> Array.toSeq |> Seq.skip 2 |> Seq.toArray)
                

                //Array.
                let execute() = 
                    printfn "executing %s with %s" Resolver.fsiPath target 
                    Process.executeProcess(Resolver.fsiPath, target) |> Process.print
//                
//                if Args.has "-h" || Args.has "--help" then
//                    let helpfilePath = m.Path.Replace(".fsx", "") + ".txt"  //this obv needs amending
//                    if System.IO.File.Exists(helpfilePath) then
//                        File.ReadAllText(helpfilePath) |> printfn "%s"
//                    else
//                        execute()
//                else
                execute()

                

| none -> printfn "no match"