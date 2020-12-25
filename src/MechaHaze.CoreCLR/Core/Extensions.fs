namespace MechaHaze.CoreCLR.Core

open System
open System.Linq
open System.Collections.Generic
open Serilog


[<AutoOpen>]
module Extensions =
    module Async =

        // TODO: Is Async.Sequential bugged? :|
        let sequentialForced<'T> =
            Seq.map (fun x -> async { return x |> Async.RunSynchronously })
            >> Async.Sequential<'T>

        let handleParallel<'T> =
            Seq.map (fun fn ->
                async {
                    try
                        do! fn
                    with ex -> Log.Error (ex, "Error running parallel task")
                })


    type IEnumerable<'T> with
        member this.FirstOrDefault (predicate, defaultValue) =
            let value =
                match predicate with
                | Some predicate -> this.FirstOrDefault predicate
                | None -> this.FirstOrDefault ()

            if Object.Equals (value, Unchecked.defaultof<'T>) then
                defaultValue ()
            else
                value

        member this.FirstOrDefault defaultValue = this.FirstOrDefault (None, defaultValue)
