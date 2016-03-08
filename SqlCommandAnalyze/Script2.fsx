﻿open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter
open Microsoft.FSharp.Reflection

type TestRecord =
    {
        AnInt : int
        AString : string
    }

let propToGetLens<'a> _ (prop : PropertyInfo) =
    let recordVar = Var("record", typeof<'a>)
    let record = Expr.Var(recordVar)
    let getMethodInfo = prop.GetGetMethod()
    let get = Expr.Call(record, getMethodInfo, [])
    Expr.Lambda(recordVar, get)

let propToSetLens<'a> index (prop : PropertyInfo) =
    let recordType = typeof<'a>
    let recordVar = Var("record", typeof<'a>)
    let record = Expr.Coerce(Expr.Var(recordVar), typeof<obj>)
    let valueVar = Var("value", prop.PropertyType)
    let value = Expr.Coerce(Expr.Var(valueVar), typeof<obj>)
    let newRecord =
        <@
            let values =
                FSharpValue.GetRecordFields((%%record : obj))
                |> List.ofArray
                |> List.mapi (fun i v ->
                    if i = index then
                        (%%value:obj)
                    else v)
                |> Array.ofList
            FSharpValue.MakeRecord((%%record).GetType(), values)
        @>
    Expr.Lambda(valueVar, Expr.Lambda(recordVar, Expr.Coerce(newRecord, recordType)))

let MakeLenses<'a> () =
    let recordType = typeof<'a>
    if not <| FSharpType.IsRecord recordType then failwith "I'm not a record"
    let fields =
        FSharpType.GetRecordFields recordType
        |> List.ofArray
    fields
    |> List.mapi (fun i f -> <@ (%%(Expr.Coerce(propToGetLens<'a> i f, typeof<obj>)):obj), (%%(Expr.Coerce(propToSetLens<'a> i f, typeof<obj>)):obj) @>)

let lenses =
    MakeLenses<TestRecord>()
    |> List.head
    |> EvaluateQuotation
    :?> obj * obj

let getLens, setLens =
    fst lenses :?> TestRecord -> int, snd lenses :?> int -> TestRecord -> TestRecord

printfn "%A" (getLens, setLens)
printfn "Get: %A" <| getLens { AnInt = 22; AString = "" }
// Get: 22
printfn "Set: %A" <| getLens (setLens 50 { AnInt = 10; AString = "" })
// Set: 50

System.Console.ReadLine() |> ignore

