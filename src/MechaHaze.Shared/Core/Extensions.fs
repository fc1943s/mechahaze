namespace MechaHaze.Shared.Core

open FSharpPlus
open FSharpPlus.Data


[<AutoOpen>]
module Extensions =
    module Result =
        let okOrThrow result =
            result
            |> Result.mapError (fun x -> x.ToString () |> exn)
            |> ResultOrException.Result

    module Set =
        let toggle item set =
            if Set.contains item set then
                Set.add item set
            else
                Set.remove item set
