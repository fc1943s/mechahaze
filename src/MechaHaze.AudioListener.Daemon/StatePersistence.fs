namespace MechaHaze.AudioListener.Daemon

open MechaHaze.Shared
open MechaHaze.Shared.CoreCLR
open Newtonsoft.Json
open Serilog
open Suigetsu.CoreCLR
open Suigetsu.Core
open System
open System.IO

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
            File.Copy (statePath, sprintf "%s.%s.error.json" statePath (DateTime.Now.ToString "yyyyMMddHHmmssfff"))
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

            File.Copy (statePath, sprintf "%s.%s.event.json" statePath (DateTime.Now.ToString "yyyyMMddHHmmssfff"))

            Ok ()
        with ex -> Error ex
