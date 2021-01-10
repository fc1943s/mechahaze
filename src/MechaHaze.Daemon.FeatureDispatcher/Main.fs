namespace MechaHaze.Daemon.FeatureDispatcher

open System.Threading
open System.Threading.Tasks
open MechaHaze.Model
open MechaHaze.CoreCLR.Core
open MechaHaze.Daemon.FeatureDispatcher
open MechaHaze.Shared
open MechaHaze.CoreCLR
open Serilog
open MechaHaze.IO
open FSharp.Control.Tasks


module Main =
    let startAsync cancellationToken =
        task {
            let configToml = SharedConfig.loadTomlIo ()

            use rabbitBus =
                RabbitQueue.createBus
                    Bridge.Endpoints.host
                    configToml.RabbitMqAddress
                    configToml.RabbitMqUsername
                    configToml.RabbitMqPassword

            let rabbitExchange = RabbitQueue.Exchange rabbitBus

            let rabbitHandlerAsync message __exchange =
                match message with
                | SharedState.StateUpdate state ->
                    Log.Debug ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)

                    OscDispatcher.updateState state
                | _ -> ()

                Task.CompletedTask

            rabbitExchange.RegisterConsumer
                [
                    "#"
                ]
                rabbitHandlerAsync
                cancellationToken
            |> ignore


            let timeSyncMapState = SafeQueue.SafeQueue<SharedState.TimeSyncMap> (fun _ _ -> Task.CompletedTask)
            timeSyncMapState.Enqueue (Map.empty |> SharedState.TimeSyncMap)

            let onOffset offset =
                let newTimeSyncMap =
                    timeSyncMapState.Dequeue ()
                    |> TimeSync.saveOffset offset

                TimeSync.getOffset newTimeSyncMap
                |> int64
                |> OscDispatcher.updateOffset

                newTimeSyncMap |> timeSyncMapState.Enqueue

            TimeSync.Client.start rabbitBus onOffset |> ignore

            let stateQueue =
                SafeQueue.SafeQueue<SharedState.SharedState> (fun oldState newState ->
                    Log.Debug ("Sending state update.\nOld: {OldState} \nNew: {NewState}", oldState, newState)

                    OscDispatcher.updateState newState

                    rabbitExchange.PostAsync "" (SharedState.ClientStateUpdate newState))

            do! OscDispatcher.hangAsync stateQueue
        }

    [<EntryPoint>]
    let main _ =
        Logging.addLoggingSink Logging.consoleSink false

        try
            try
                (startAsync CancellationToken.None)
                    .GetAwaiter()
                    .GetResult()

                Log.Information ("Program end")
                0
            with ex ->
                Log.Error (ex, "Program error")
                1
        finally
            Log.CloseAndFlush ()
