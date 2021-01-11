namespace MechaHaze.Daemon.AudioListener

open System.Threading
open FSharp.Control.Tasks
open System.Threading.Tasks
open FSharpPlus
open MechaHaze.Model
open MechaHaze.Shared.Core
open MechaHaze.IO
open MechaHaze.Shared
open Serilog
open MechaHaze.Daemon.AudioListener
open Elmish
open MechaHaze.CoreCLR
open Newtonsoft.Json
open MechaHaze.CoreCLR.Core


module Main =
    let start cancellationToken =
        task {
            let configToml = SharedConfig.loadTomlIo ()

            let eventInjectorQueue = SafeQueue.SafeQueue<LocalQueue.Event -> unit> (fun _ _ -> Task.CompletedTask)

            let eventInjectorSub dispatch =
                Log.Debug ("SUBSCRIPTION STARTED. CAN DISPATCH")
                eventInjectorQueue.Enqueue dispatch


            let rabbitExchange =
                let rabbitBus =
                    RabbitQueue.createBus
                        Bridge.Endpoints.host
                        configToml.RabbitMqAddress
                        configToml.RabbitMqUsername
                        configToml.RabbitMqPassword

                TimeSync.Server.startAsync rabbitBus |> ignore

                let rabbitExchange = RabbitQueue.Exchange rabbitBus

                let rabbitHandlerAsync message __exchange =
                    match message with
                    | SharedState.ClientStateUpdate state ->
                        Log.Debug ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)

                        let dispatch = eventInjectorQueue.Dequeue ()
                        dispatch (LocalQueue.ClientSetState state)
                    | _ -> ()

                    Task.CompletedTask

                rabbitExchange.RegisterConsumer
                    [
                        "#"
                    ]
                    rabbitHandlerAsync
                    cancellationToken
                |> ignore

                rabbitExchange


            let stateUri = StatePersistence.stateUriMemoizedLazy ()

            let init () =
                let state =

                    match (StatePersistence.read stateUri)
                        .GetAwaiter()
                        .GetResult() with
                    | Ok state ->
                        (rabbitExchange.PostAsync "" (SharedState.StateUpdate state))
                            .GetAwaiter()
                            .GetResult()

                        state

                    | Error ex ->
                        Log.Error
                            (ex,
                             "Error loading state. Default: {A}.",
                             JsonConvert.SerializeObject SharedState.SharedState.Default)

                        raise ex

                state, Cmd.none


            let trackMatcherStabilizerIo = TrackMatcher.Stabilizer.Io.create ()


            let update (message: LocalQueue.Event)
                       (state: SharedState.SharedState)
                       : SharedState.SharedState * Cmd<LocalQueue.Event> =
                let newState, newCmd =
                    match message with
                    | LocalQueue.Error ex ->
                        Log.Debug (ex, "Error while preparing cmd")
                        state, Cmd.none

                    | LocalQueue.RecordingRequest ->
                        let cmd =
                            match state.RecordingMode, state.Track.Locked with
                            | false, _
                            | _, false ->
                                AudioRecorder.recordIoAsync
                                |> Async.map (Result.either LocalQueue.Recording LocalQueue.Error)
                                |> Cmd.OfAsync.result
                            | _ -> Cmd.none

                        state, cmd

                    | LocalQueue.Recording sample ->
                        let cmd =
                            Cmd.OfAsync.either TrackMatcher.querySampleIoAsync sample LocalQueue.Match LocalQueue.Error

                        state, cmd

                    | LocalQueue.Match unstableTrack ->
                        let stableTrack =
                            unstableTrack
                            |> TrackMatcher.Stabilizer.stabilizeTrack state.Track
                            |> Io.run trackMatcherStabilizerIo
                            |> fst

                        let newState =
                            match stableTrack with
                            | None ->
                                Log.Verbose ("No stable track found.")
                                { state with Track = SharedState.Track.Default }

                            | Some stableTrack ->

                                if unstableTrack.Id <> stableTrack.Id then
                                    Log.Debug (">>> State   . '{A}' {B}", state.Track.Id, state.Track.Position)
                                    Log.Debug (">>> Unstable. '{A}' {B}", unstableTrack.Id, unstableTrack.Position)
                                    Log.Debug (">>> Stable  . '{A}' {B}", stableTrack.Id, stableTrack.Position)

                                    TrackLocker.reset ()

                                    { state with
                                        Track = { stableTrack with Locked = false; Offset = 0. }
                                    }

                                elif state.Track.Locked || stableTrack.Position <= 0. then
                                    Log.Debug ("Track locked or with position 0")
                                    state
                                else
                                    let locked, offset = TrackLocker.lockTrack state stableTrack

                                    Log.Debug
                                        ("Track locking: {A}. Id: {B}. Offset: {C}", locked, stableTrack.Id, offset)

                                    { state with
                                        Track =
                                            { stableTrack with
                                                Locked = locked
                                                Offset = offset
                                            }
                                    }

                        // rabbitQueue.Post (SharedState.TrackUpdate newState.Track)
                        rabbitExchange.PostAsync "" (SharedState.StateUpdate newState)

                        newState, Cmd.none

                    | LocalQueue.ClientSetState newState ->
                        // TODO: this if...
                        if newState.PresetList <> state.PresetList then
                            //                    Log.Debug ("Setting new time sync offset: {A}", newState.TimeSync)
                            rabbitExchange.PostAsync "" (SharedState.StateUpdate newState)
                        else
                            Task.CompletedTask

                        newState, Cmd.none

                let cleanState, cleanNewState = (state, newState) |> Tuple2.map SharedState.clean

                if cleanState <> cleanNewState then
                    Log.Debug ("Persisting new state")

                    match (cleanNewState |> StatePersistence.write stateUri)
                        .GetAwaiter()
                        .GetResult() with
                    | Error ex ->
                        Log.Error
                            (ex,
                             "Error persisting state. Default: {A}",
                             JsonConvert.SerializeObject SharedState.SharedState.Default)
                    | Ok () -> ()

                newState, newCmd

            let timerSub dispatch =
                let timerEventAsync () =
                    dispatch LocalQueue.RecordingRequest
                    Task.CompletedTask

                timerEventAsync
                |> Timer.hangAsync (int SharedState.sampleIntervalMs)
                |> ignore

            let view __state __dispatch = ()

            let subscription _state =
                Cmd.batch [
                    Cmd.ofSub (Sub timerSub) LocalQueue.Error
                    Cmd.ofSub (Sub eventInjectorSub) LocalQueue.Error
                ]

            let updateWrapper message state =
                try
                    update message state
                with ex ->
                    Log.Error (ex, "State update error")
                    state, Cmd.none

            Program.mkProgram init updateWrapper view
            |> Program.withSubscription subscription
            |> Program.run

            while true do
                do! Task.Delay 1000
        }

    [<EntryPoint>]
    let main _ =
        start(CancellationToken.None).GetAwaiter()
            .GetResult
        |> Startup.withLogging false
