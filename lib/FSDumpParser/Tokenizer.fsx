open System
//from http://blog.efvincent.com/parsing-json-using-f/

type Operator =
    | Equals

type Token =
  | OpenCurlyBracket | CloseCurlyBracket
  | OpenBracket | CloseBracket
  | OpenList | CloseList
  | OpenArray | CloseArray
  | Colon | Comma | Semicolon
  | Operator of Operator
  | String of string
  | UnquotedString of string
  | Number of string

let tokenize source = 
    let chars = source |> Seq.map id |> Seq.toList
    //let cl = source |> 
    let rec parseString acc = function
    | '\\' :: '"' :: t -> // escaped quote
                            parseString (acc + "\"") t
    | '"' :: t -> // closing quote terminates
                    acc, t
    | c :: t -> // otherwise accumulate
                parseString (acc + (c.ToString())) t
    | _ -> failwith "Malformed string."

    let rec parseFn acc = function
    | c :: t when Char.IsLetter(c) -> // otherwise accumulate
                parseFn (acc + (c.ToString())) t
    | x :: t -> // closing quote terminates
                acc,  x::t
    | ')' :: t -> // closing quote terminates
                    acc, ')':: t
    | _ -> failwith "Malformed string."

    let rec token acc = function
    | (')' :: _) as t -> acc, ')':: t // closing paren terminates
    | (':' :: _) as t -> acc, ':':: t // colon terminates
    | (',' :: _) as t -> acc, ',':: t // comma terminates
    | w :: t when Char.IsWhiteSpace(w) -> acc, t // whitespace terminates
    | [] -> acc, [] // end of list terminates
    | c :: t -> token (acc + (c.ToString())) t // otherwise accumulate chars 

    let rec tokenize' acc = function
    | w :: t when Char.IsWhiteSpace(w) -> tokenize' acc t   // skip whitespace
    | '{' :: t -> tokenize' (OpenCurlyBracket :: acc) t
    | '}' :: t -> tokenize' (CloseCurlyBracket :: acc) t
    | '(' :: t -> tokenize' (OpenBracket :: acc) t
    | ')' :: t -> tokenize' (CloseBracket :: acc) t
    | '[' :: '|' :: t -> tokenize' (OpenArray :: acc) t
    | '|' :: ']' :: t -> tokenize' (CloseArray :: acc) t
    | '[' :: t -> tokenize' (OpenList :: acc) t
    | ']' :: t -> tokenize' (CloseList :: acc) t
    | ':' :: t -> tokenize' (Colon :: acc) t
    | ';' :: t -> tokenize' (Semicolon :: acc) t
    | '=' :: t -> tokenize' (Operator(Equals) :: acc) t
    | ',' :: t -> tokenize' (Comma :: acc) t
    | '"' :: t -> // start of string
        let s, t' = parseString "" t
        tokenize' (Token.String(s) :: acc) t'
    | '-' :: d :: t when Char.IsDigit(d) -> // start of negative number
        let n, t' = token ("-" + d.ToString()) t
        tokenize' (Token.Number(n) :: acc) t'
    | '+' :: d :: t | d :: t when Char.IsDigit(d) -> // start of positive number
        let n, t' = token (d.ToString()) t
        tokenize' (Token.Number(n) :: acc) t'
    | [] -> List.rev acc // end of list terminates
    | c :: t when Char.IsLetter(c) -> 
        let s, t' = parseFn (c.ToString()) t
        tokenize' (Token.UnquotedString(s) :: acc) t'
    | _ -> failwith "Tokinzation error"

    tokenize' [] chars

//tokenize "{ \"abcggg\":\"42\"}"

tokenize "[| (Rabbit(Test({Timmy=\"test\"})) |]"

let str = """[|
        (Script({Name="ratstew"; FileType=Fsx; Path="path";Priority=1}));
        (Library({Name="file"; FileType=Fsx; Path="path";Priority=1}));
        (Script({Name="file"; FileType=Fsx; Path="path";Priority=3}))
    |]"""

tokenize str