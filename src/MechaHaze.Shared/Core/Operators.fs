namespace MechaHaze.Shared.Core

open System.IO


[<AutoOpen>]
module Operators =
    let inline (><) x (min, max) = (x > min) && (x < max)

    let inline (>=<) x (min, max) = (x >= min) && (x < max)

    let inline (>==<) x (min, max) = (x >= min) && (x <= max)
    let (</>) a b = Path.Combine (a, b)
