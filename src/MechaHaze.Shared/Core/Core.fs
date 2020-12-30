namespace MechaHaze.Shared.Core

open System


module Core =
    let getTimestamp (date: DateTime) = date.ToString "yyyyMMddHHmmssfff"

    let memoizeLazy fn =
        let result = lazy (fn ())
        fun () -> result.Value
