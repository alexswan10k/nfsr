#load "Tokenizer.fsx"

open Tokenizer

type AggregatedToken = 
    | Placeholder
    //| Assignment of string * string
    //| Const of string
    //| Token of Token
    //| Call of string * AggregatedToken
    | Group of list<AggregatedToken>
    | Array of list<AggregatedToken>
    | Record of list<AggregatedToken>
    | Call of string
    | String of string
    | Operator of Operator

let aggregateTokens tokens =
    let rec aggr acc = function
        | Tokenizer.UnquotedString(s) :: t ->
            aggr (Call(s):: acc) t
        | Tokenizer.String(s) :: t ->
            aggr (String(s):: acc) t
        | Tokenizer.Operator(o) :: t ->
            aggr (Operator(o):: acc) t
        | Tokenizer.OpenArray :: t -> 
            let acc', t' = aggr list.Empty t
            aggr (Array(acc') :: acc) t'
        | Tokenizer.Semicolon :: t -> 
            aggr (acc) t 
        | Tokenizer.CloseArray :: t ->
            List.rev acc, t

        | Tokenizer.OpenBracket :: t -> 
            let acc', t' = aggr list.Empty t  //a group should only ever contain 1 token
            aggr (Group(acc') :: acc) t'
        | Tokenizer.CloseBracket :: t ->
            List.rev acc, t

        | Tokenizer.OpenCurlyBracket :: t -> 
            let acc', t' = aggr list.Empty t  //a group should only ever contain 1 token
            aggr (Record(acc') :: acc) t'
        | Tokenizer.CloseCurlyBracket :: t ->
            List.rev acc, t

        //| Semicolon :: t -> parse' (Placeholder :: acc) t //so this would split array components?
        | x :: t -> aggr (Placeholder:: acc) t
        | t -> 
            List.rev acc, t  //end of seq?


    let acc, t = aggr [] tokens
    acc

let p = Tokenizer.tokenize str
aggregateTokens p

let parser<'t> aggregatedTokens =
    
    let build (t:System.Type) (args : obj[]) =
        System.Activator.CreateInstance(t, args)
        
//    let findType = function
//        | "array" -> typedefof<array<_>>
//        | name -> 
//        | _ -> failwith "rats"
//        typedefof<'t>
    let rec parser' acc = function
        | Array(stuff) :: t -> 
            let acc', t' = parser' [] stuff
            acc, t
        | _ -> failwith "oh dear"
    parser' aggregatedTokens |> ignore
    
    ()