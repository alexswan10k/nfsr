let elementsAfter compareList list =
    let compareSeq = compareList |> List.toSeq |> Seq.rev
    let rec walk l =
        match l with
        | a::x -> 
            let cutSeq = (x |> List.toSeq)
            let isMatch =Seq.zip cutSeq compareSeq
                            |> Seq.fold (fun acc (a, b) -> acc && a = b) true
                            && x.Length > 0
            if isMatch then
                Some(x.Length)
            else
                walk x
        | _ -> None
    match walk (list |> List.rev) with
    | Some(length) -> List.skip length list
    | None -> []

let (|ElementsAfter|) compareList list =
     elementsAfter compareList list

// let aList = ["A"; "B"; "C"; "D"; "E"];
// let hasMatch =
//     match aList with
//     | ElementsAfter ["A";"B"] (c::d::x) -> Some (c, d)
//     | ElementsAfter ["C"; "D"] (e::f::g)-> Some (e,e)
//     | _ -> None