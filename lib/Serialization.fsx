#r "System.Runtime.Serialization.dll"
//#r "System.Web.Extensions"
open System
open Microsoft.FSharp.Reflection
open System.IO
open System.Reflection
open System.Runtime.Serialization
open System.Runtime.Serialization.Json

//https://gist.github.com/theburningmonk/2071722 
let toString = System.Text.Encoding.ASCII.GetString
let toBytes (x : string) = System.Text.Encoding.ASCII.GetBytes x
let serializeJson<'a> (x : 'a) = 
    let jsonSerializer = new DataContractJsonSerializer(typedefof<'a>)

    use stream = new MemoryStream()
    jsonSerializer.WriteObject(stream, x)
    toString <| stream.ToArray()

let deserializeJson<'a> (json : string) =
    let jsonSerializer = new DataContractJsonSerializer(typedefof<'a>)

    use stream = new MemoryStream(toBytes json)
    jsonSerializer.ReadObject(stream) :?> 'a

let knownTypesForUnion<'a> =
    typedefof<'a>.GetNestedTypes(BindingFlags.Public ||| BindingFlags.NonPublic)
        |> Array.filter FSharpType.IsUnion

//let serializeJson2<'a> (x: 'a) =
//    let serializer = System.Web.Script.Serialization.JavaScriptSerializer()
//    serializer.Serialize(x)
//
//let deserializeJson2<'a> (json : string) =
//    let serializer = System.Web.Script.Serialization.JavaScriptSerializer()
//    serializer.Deserialize<'a>(json)

let serializeByte<'a> (x: 'a) =
    let bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
    use ms = new MemoryStream()
    bf.Serialize(ms, x)
    ms.GetBuffer()

let deserializeByte<'a>(packet: byte[]) =
    let bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
    use ms = new MemoryStream(packet)
    bf.Deserialize(ms) |> unbox<'a>