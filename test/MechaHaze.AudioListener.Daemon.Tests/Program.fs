namespace MechaHaze.AudioListener.Daemon

open System.Diagnostics.CodeAnalysis

open Expecto

module Program =

    [<ExcludeFromCodeCoverage>]
    [<EntryPoint>]
    let main args =
        runTestsWithArgs defaultConfig args Tests.tests

