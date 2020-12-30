namespace MechaHaze.UI.Backend

open MechaHaze.IO
open MechaHaze.Model
open MechaHaze.Shared
open MechaHaze.CoreCLR
open MechaHaze.UI.Backend
open MechaHaze.UI.Backend.ElmishBridge
open MechaHaze.CoreCLR.Core
open Serilog
open Elmish.Bridge
open Elmish
open MechaHaze.CoreCLR.Core
open MechaHaze.Model
open Serilog
open Giraffe.SerilogExtensions
open MechaHaze.UI


module Main =
    let startAsync =
        async {
            let configToml = SharedConfig.loadTomlIo ()

            let uiServer = UIServer.UIServer ()

            let mutable _rabbitExchange: RabbitQueue.Exchange<SharedState.SharedQueue> option = None
            let mutable _stateQueue: SafeQueue.SafeQueue<Server.StateScope<UIState.State>> option = None

            let stateBroadcastQueue =
                SafeQueue.SafeQueue<Server.StateScope<SharedState.SharedState>> (fun _ newState ->
                    async {
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

                            | Server.Remote state -> rabbitExchange.Post "" (SharedState.ClientStateUpdate state)
                        | _ -> ()
                    })

            use rabbitBus =
                RabbitQueue.createBus
                    Bridge.Endpoints.host
                    configToml.RabbitMqAddress
                    configToml.RabbitMqUsername
                    configToml.RabbitMqPassword

            let rabbitHandlerAsync message __exchange =
                async {
                    match message with
                    | SharedState.StateUpdate state ->
                        Log.Debug ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)

                        stateBroadcastQueue.Enqueue (Server.Internal state)

                    | _ -> ()
                }

            _rabbitExchange <-
                let rabbitExchange = RabbitQueue.Exchange rabbitBus

                rabbitExchange.RegisterConsumer
                    [
                        "#"
                    ]
                    rabbitHandlerAsync

                Some rabbitExchange

            let stateQueue =
                SafeQueue.SafeQueue<Server.StateScope<UIState.State>> (fun _ newState ->
                    async {
                        match newState with
                        | Server.Remote newState -> stateBroadcastQueue.Enqueue (Server.Remote newState.SharedState)
                        | _ -> ()
                    })

            _stateQueue <- Some stateQueue

            let __waitForInitialState = stateQueue.Dequeue ()

            TimeSync.Client.start rabbitBus (fun offset ->
                let state = stateQueue.Dequeue () |> Server.scopeToState

                let newTimeSyncMap = state.TimeSyncMap |> TimeSync.saveOffset offset

                let newState = { state with TimeSyncMap = newTimeSyncMap }
                stateQueue.Enqueue (Server.Internal newState))

            do! uiServer.HangAsync stateQueue
        }

    [<EntryPoint>]
    let main _ =
        fun () -> startAsync |> Async.RunSynchronously
        |> Startup.withLogging false
