#load "Serialization.fsx"
open System
open System.IO

type FileStore<'a>(path) =
    member x.Get() =
        if File.Exists(path) then
            File.ReadAllText(path)
                |> Serialization.deserializeJson<'a>
                |> Some
        else None

    member x.Set (item : 'a) =
        File.WriteAllText(path, Serialization.serializeJson(item))

    member x.GetOrCreate (buildItem : unit -> 'a) =
        match x.Get() with
        | Some(cval) -> cval
        | None -> 
            let cval = buildItem()
            x.Set(cval)
            cval