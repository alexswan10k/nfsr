#load "..\..\lib\Args.fsx"

printfn "Please enter your name"

let name = System.Console.ReadLine()
printfn "Hello %s" name