namespace MechaHaze.Shared.Core

module Tuple2 =
    let map fn (a, b) = fn a, fn b
