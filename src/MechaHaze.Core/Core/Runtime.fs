namespace MechaHaze.Core

open System

module Runtime =
    let getStackTrace () =
        Environment.StackTrace.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries)
