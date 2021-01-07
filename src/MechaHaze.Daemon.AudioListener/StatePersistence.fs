namespace MechaHaze.Daemon.AudioListener

open System
open MechaHaze.Shared
open MechaHaze.CoreCLR
open Serilog
open MechaHaze.Shared.Core
open System.IO
open MechaHaze.CoreCLR.Core
open FSharp.Control.Tasks


module StatePersistence =
    type StateUri = StateUri of uri: Uri

    let stateUriMemoizedLazy =
        fun () ->
            (SharedConfig.pathsLazyIo ()).dbState
            </> "main.json"
            |> Uri
            |> StateUri
        |> Core.memoizeLazy

    let read (StateUri uri) =
        task {
            try
                let! json = File.ReadAllTextAsync uri.AbsolutePath

                return
                    Json.deserialize<SharedState.SharedState> json
                    |> Ok
            with ex ->
                File.Copy (uri.AbsolutePath, $"{uri.AbsolutePath}.{Core.getTimestamp DateTime.Now}.error.json")
                return Error ex
        }


    let write (StateUri uri) (newState: SharedState.SharedState) =
        task {
            try
                let json = Json.serialize newState

                uri.AbsolutePath
                |> Path.GetDirectoryName
                |> Directory.CreateDirectory
                |> ignore

                do! File.WriteAllTextAsync (uri.AbsolutePath, json)

                File.Copy (uri.AbsolutePath, $"{uri.AbsolutePath}.{Core.getTimestamp DateTime.Now}.event.json")

                return Ok ()
            with ex -> return Error ex
        }
