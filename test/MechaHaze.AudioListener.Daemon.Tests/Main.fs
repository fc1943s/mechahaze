namespace MechaHaze.AudioListener.Daemon

open System.Diagnostics.CodeAnalysis

open Expecto

module Main =

    [<ExcludeFromCodeCoverage>]
    [<EntryPoint>]
    let main args = runTestsWithArgs defaultConfig args Tests.tests
