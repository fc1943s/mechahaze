namespace MechaHaze.Core

module Core =
    let memoizeLazy fn =
        let result = lazy (fn ())
        fun () -> result.Value

