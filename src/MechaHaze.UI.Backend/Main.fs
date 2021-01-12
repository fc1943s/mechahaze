namespace MechaHaze.UI.Backend

open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks
open MechaHaze.IO
open MechaHaze.Model
open MechaHaze.Shared
open MechaHaze.Shared.Core
open MechaHaze.CoreCLR
open MechaHaze.UI.Backend
open MechaHaze.UI.Backend.ElmishBridge
open MechaHaze.CoreCLR.Core
open Serilog
open Elmish.Bridge
open Elmish
open MechaHaze.UI


module Main =
    let startAsync cancellationToken =
        task {
            let! configToml = SharedConfig.loadToml ()
            let configToml = configToml |> Result.unwrap

            let uiServer = UIServer.UIServer ()

            let mutable _rabbitExchange: RabbitQueue.Exchange<SharedState.SharedQueue> option = None
            let mutable _stateQueue: SafeQueue.SafeQueue<Server.StateScope<UIState.State>> option = None

            let stateBroadcastQueue =
                SafeQueue.SafeQueue<Server.StateScope<SharedState.SharedState>> (fun _ newState ->
                    match _rabbitExchange, _stateQueue with
                    | Some rabbitExchange, Some stateQueue ->
                        match newState with
                        | Server.Internal _ ->
                            let oldState =
                                if stateQueue.IsEmpty () then
                                    UIState.State.Default
                                else
                                    stateQueue.Dequeue () |> Server.scopeToState

                            //                                uiServer.BroadcastState
//                                    { oldState with
//                                        SharedState = newState |> Server.scopeToState
//                                    }

                            stateQueue.Enqueue
                                (Server.Internal
                                    { oldState with
                                        SharedState = newState |> Server.scopeToState
                                    })

                            Task.CompletedTask

                        | Server.Remote state -> rabbitExchange.PostAsync "" (SharedState.ClientStateUpdate state)
                    | _ -> Task.CompletedTask)

            use rabbitBus =
                RabbitQueue.createBus
                    Bridge.Endpoints.host
                    configToml.RabbitMqAddress
                    configToml.RabbitMqUsername
                    configToml.RabbitMqPassword

            let rabbitHandlerAsync message __exchange =
                match message with
                | SharedState.StateUpdate state ->
                    Log.Debug ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)

                    stateBroadcastQueue.Enqueue (Server.Internal state)

                | _ -> ()

                Task.CompletedTask

            _rabbitExchange <-
                let rabbitExchange = RabbitQueue.Exchange rabbitBus

                rabbitExchange.RegisterConsumer
                    [
                        "#"
                    ]
                    rabbitHandlerAsync
                    cancellationToken
                |> ignore

                Some rabbitExchange

            let stateQueue =
                SafeQueue.SafeQueue<Server.StateScope<UIState.State>> (fun _ newState ->
                    match newState with
                    | Server.Remote newState -> stateBroadcastQueue.Enqueue (Server.Remote newState.SharedState)
                    | _ -> ()

                    Task.CompletedTask)

            _stateQueue <- Some stateQueue

            let __waitForInitialState = stateQueue.Dequeue ()

            TimeSync.Client.start rabbitBus (fun offset ->
                let state = stateQueue.Dequeue () |> Server.scopeToState

                let newTimeSyncMap = state.TimeSyncMap |> TimeSync.saveOffset offset

                let newState = { state with TimeSyncMap = newTimeSyncMap }
                stateQueue.Enqueue (Server.Internal newState))
            |> ignore

            do! uiServer.HangAsync stateQueue
        }

    [<EntryPoint>]
    let main _ =
        startAsync(CancellationToken.None).GetAwaiter()
            .GetResult
        |> Startup.withLogging false
