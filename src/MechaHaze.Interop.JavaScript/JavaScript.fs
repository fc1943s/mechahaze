﻿namespace MechaHaze.Interop.JavaScript

module JavaScript =
    let newJsArray array = array |> Array.toList |> List.toArray
