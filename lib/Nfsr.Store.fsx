//#load "Serialization.fsx"
open System
open System.IO

type FileStore<'a>(path, serialize: ('a -> string), deserialize:(string -> 'a)) =
    member x.Get() =
        if File.Exists(path) then
//            File.ReadAllText(path)
//                |> Serialization.deserializeJson<'a>
//                |> Some
//            File.ReadAllBytes(path)
//                |> Serialization.deserializeByte<'a>
//                |> Some
            let text = File.ReadAllText(path)
//            let res = SharpXml.XmlSerializer.DeserializeFromString<option<'a>>(text)
//            res
            Some(deserialize(text))
        else None

    member x.Set (item : 'a) =
        //File.WriteAllText(path, Serialization.serializeJson(item))
        //File.WriteAllBytes(path, Serialization.serializeByte(item))
//        let text = SharpXml.XmlSerializer.SerializeToString(item)
//        File.WriteAllText(path, text)
        let text = serialize(item)
        File.WriteAllText(path, text)

    member x.GetOrCreate (buildItem : unit -> 'a) =
        match x.Get() with
        | Some(cval) -> cval
        | None -> 
            let cval = buildItem()
            x.Set(cval)
            cval