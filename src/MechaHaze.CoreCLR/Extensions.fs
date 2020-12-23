namespace MechaHaze.CoreCLR

open System
open System.Linq
open System.Collections.Generic


[<AutoOpen>]
module Extensions =

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
