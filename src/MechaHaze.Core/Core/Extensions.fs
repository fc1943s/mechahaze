namespace MechaHaze.Core

open FSharpPlus
open FSharpPlus.Data


[<AutoOpen>]
module Extensions =
    module Result =
        let okOrThrow result =
            result
            |> Result.mapError (fun x -> x.ToString () |> exn)
            |> ResultOrException.Result

