namespace MechaHaze.Daemon.AudioListener

open System
open MechaHaze.Shared
open MechaHaze.CoreCLR
open Newtonsoft.Json
open Serilog
open MechaHaze.Shared.Core
open System.IO
open MechaHaze.CoreCLR.Core


module StatePersistence =
    let private statePathLazyIo =
        fun () -> Path.Combine ((SharedConfig.pathsLazyIo ()).dbState, "main.json")
        |> Core.memoizeLazy

    let readIo () =
        let statePath = statePathLazyIo ()

        try
            let json = File.ReadAllText statePath

            let settings = JsonSerializerSettings (ContractResolver = Json.contractResolvers.RequireObjectProperties)

            (json, settings)
            |> JsonConvert.DeserializeObject<SharedState.SharedState>
            |> Ok
        with ex ->
            File.Copy (statePath, $"{statePath}.{Core.getTimestamp DateTime.Now}.error.json")

            Error ex

    let writeIo (newState: SharedState.SharedState) =
        try
            let json = JsonConvert.SerializeObject (newState, Formatting.Indented)

            let statePath = statePathLazyIo ()

            statePath
            |> Path.GetDirectoryName
            |> Directory.CreateDirectory
            |> ignore

            File.WriteAllText (statePath, json)

            File.Copy (statePath, $"{statePath}.{Core.getTimestamp DateTime.Now}.event.json")

            Ok ()
        with ex -> Error ex