#load "dependencies.fsx"

open Expecto
open Expecto.Flip

open System
open System.IO
open MechaHaze.Daemon.AudioListener
open MechaHaze.CoreCLR.Core
open MechaHaze.Shared.Core
open MechaHaze.Shared
open System.Threading.Tasks
open System.Threading
open FSharp.Control.Tasks

let tests =
    testList
        "tests"
        [
            testList
                (nameof StatePersistence)
                [
                    test "Save then load state (default)" {
                        (task {
                            let dir = FileSystem.ensureTempSessionDirectory ()

                            let statePath = StatePersistence.StateUri (Uri (dir </> "state.json"))
                            let initialState = SharedState.SharedState.Default
                            let! writeResult = initialState |> StatePersistence.write statePath
                            writeResult |> Result.unwrap
                            match! StatePersistence.read statePath with
                            | Ok newState -> newState |> Expect.equal "" initialState
                            | Error ex -> raise ex

                            Directory.Delete (dir, true)
                         })
                            .GetAwaiter()
                            .GetResult()
                    }
                    test "Save then load state (current)" {
                        (task {
                            let stateUri = StatePersistence.stateUriMemoizedLazy ()

                            match! StatePersistence.read stateUri with
                            | Ok initialState ->
                                let dir = FileSystem.ensureTempSessionDirectory ()
                                let statePath = StatePersistence.StateUri (Uri (dir </> "state.json"))
                                let! writeResult = initialState |> StatePersistence.write statePath
                                writeResult |> Result.unwrap

                                match! StatePersistence.read statePath with
                                | Ok newState -> newState |> Expect.equal "" initialState
                                | Error ex -> raise ex

                                Directory.Delete (dir, true)
                            | Error ex -> raise ex
                         })
                            .GetAwaiter()
                            .GetResult()
                    }
                ]
        ]


runTests { defaultConfig with verbosity = Logging.Debug } tests
