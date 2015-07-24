open System.IO

open System.Diagnostics
open System.Text.RegularExpressions

type ProcessResult = { exitCode : int; stdout : string; stderr : string; output: string[] }

let executeProcess (exe,cmdline) =
    let psi = new System.Diagnostics.ProcessStartInfo(exe,cmdline) 
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.CreateNoWindow <- true        
    let p = System.Diagnostics.Process.Start(psi) 
    let output = new System.Text.StringBuilder()
    let outputList = new System.Collections.Generic.List<string>();
    let error = new System.Text.StringBuilder()
    p.OutputDataReceived.Add(fun args -> 
                                output.Append(args.Data) |> ignore
                                outputList.Add(args.Data))
    p.ErrorDataReceived.Add(fun args -> error.Append(args.Data) |> ignore)
    p.BeginErrorReadLine()
    p.BeginOutputReadLine()
    p.WaitForExit()
    { exitCode = p.ExitCode; stdout = output.ToString(); stderr = error.ToString(); output = outputList.ToArray() }

let shellExecute cmd =
    executeProcess("cmd.exe", "/c \"" + cmd + "\"").output