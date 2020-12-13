namespace MechaHaze.AudioListener.Daemon

open MechaHaze.Shared
open Serilog
open Suigetsu.Bus
open Suigetsu.CoreCLR
open Suigetsu.Core
open FSharp.Control
open MechaHaze.AudioListener.Daemon
open Elmish
open MechaHaze.Shared.CoreCLR
open Newtonsoft.Json
open FSharpPlus

module Main =
    let startAsync =
        async {
            let configToml = SharedConfig.loadTomlIo ()

            let eventInjectorQueue = SafeQueue.SafeQueue<LocalQueue.Event -> unit> (fun _ _ -> async { () })

            let eventInjectorSub dispatch =
                Log.Debug ("SUBSCRIPTION STARTED. CAN DISPATCH")
                eventInjectorQueue.Enqueue dispatch


            let rabbitExchange =
                let rabbitBus = RabbitQueue.createBus configToml.RabbitMqAddress "root" "root"

                TimeSync.Server.start rabbitBus

                let rabbitExchange = RabbitQueue.Exchange rabbitBus

                let rabbitHandlerAsync message __exchange =
                    async {
                        match message with
                        | SharedState.ClientStateUpdate state ->
                            Log.Debug
                                ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)

                            let dispatch = eventInjectorQueue.Dequeue ()
                            dispatch (LocalQueue.ClientSetState state)
                        | _ -> ()
                    }

                rabbitExchange.RegisterConsumer
                    [
                        "#"
                    ]
                    rabbitHandlerAsync

                rabbitExchange



            let init () =
                let state =
                    match StatePersistence.readIo () with
                    | Ok state ->
                        rabbitExchange.Post "" (SharedState.StateUpdate state)
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
                        rabbitExchange.Post "" (SharedState.StateUpdate newState)

                        newState, Cmd.none

                    | LocalQueue.ClientSetState newState ->
                        // TODO: this if...
                        if newState.BindingsPresetMap
                           <> state.BindingsPresetMap then
                            //                    Log.Debug ("Setting new time sync offset: {A}", newState.TimeSync)
                            rabbitExchange.Post "" (SharedState.StateUpdate newState)

                        newState, Cmd.none

                let cleanState, cleanNewState = (state, newState) |> Tuple2.map SharedState.clean

                if cleanState <> cleanNewState then
                    Log.Debug ("Persisting new state")

                    match StatePersistence.writeIo cleanNewState with
                    | Error ex ->
                        Log.Error
                            (ex,
                             "Error persisting state. Default: {A}",
                             JsonConvert.SerializeObject SharedState.SharedState.Default)
                    | Ok () -> ()

                newState, newCmd

            let timerSub dispatch =
                let timerEventAsync = async { dispatch LocalQueue.RecordingRequest }

                timerEventAsync
                |> Timer.hangAsync (int SharedState.sampleIntervalMs)
                |> Async.Start

            let view __state __dispatch = ()

            let subscription state =
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
                do! Async.Sleep 1000
        }

    [<EntryPoint>]
    let main _ =
        fun () -> startAsync |> Async.RunSynchronously
        |> Startup.withLogging false
