#load "..\lib\Process.fsx"
#load "..\lib\_Nfsr.fsx"

open System.IO

let args = Args.getArgs() |> Seq.skip 1 |> Seq.toArray

if args.Length < 2 then
    File.ReadAllText(Resolver.globalBasePath + "\\nfsr\\bin\\nfsr.txt") |> printfn "%s"
else
    let headParams = _Nfsr.getHeadParams (args)
    let allowedTypes = _Nfsr.getAllowedTypes (headParams)

    let cmatch = 
        Resolver.getClosestMatch (args.[headParams.Length]) allowedTypes

    match cmatch with
    | Some(m) ->    
                    let join (arr: string[]) = System.String.Join(" ", arr)
                    let fullPath = Resolver.fsiPath + " " + m.Path
                    let target = m.Path + " " + join (args |> Array.toSeq |> Seq.skip (headParams.Length + 1) |> Seq.toArray)
                

                    //Array.
                    let execute() = 
                        if m.FileType = Resolver.FileType.Fsx then
                            Process.executeProcess(Resolver.fsiPath, target) |> Process.print
                        else if m.FileType = Resolver.FileType.Powershell then
                            Process.executeProcess(Resolver.powershellPath, "-ExecutionPolicy Bypass -File "+ target) |> Process.print
                            //Process.shellExecute("powershell -ExecutionPolicy Bypass -File "+ target) |> Process.print
                        else
                            Process.shellExecute(target) |> Process.print
                
                    if Args.hasFor "-h" headParams || Args.hasFor "--help" headParams then
                        let helpfilePath = m.Path
                                            .Replace(".fsx", "")
                                            .Replace(".bat", "")
                                            .Replace(".ps1", "")
                                            .Replace(".sh", "") + ".txt"
                        if System.IO.File.Exists(helpfilePath) then
                            File.ReadAllText(helpfilePath) |> printfn "%s"
                        else
                            execute()
                    else
                    execute()

                

    | none -> printfn "no match"