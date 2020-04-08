namespace MechaHaze.UI.Backend

open MechaHaze.Shared
open MechaHaze.Shared.CoreCLR
open MechaHaze.UI
open MechaHaze.UI.Backend
open Serilog
open Suigetsu.CoreCLR
open Suigetsu.Core
open Suigetsu.Bus
open Suigetsu.UI.Backend.ElmishBridge

module Program =
    let startAsync = async {
        let configToml = SharedConfig.loadTomlIo ()
        
        let uiServer = UIServer.UIServer()
        
        let mutable _rabbitExchange : RabbitQueue.Exchange<SharedState.SharedQueue> option = None
        let mutable _stateQueue : SafeQueue.SafeQueue<Server.StateScope<UIState.State>> option = None
        
        let stateBroadcastQueue = SafeQueue.SafeQueue<Server.StateScope<SharedState.SharedState>> (fun _ newState -> async {
            match _rabbitExchange, _stateQueue with
            | Some rabbitExchange, Some stateQueue ->
                match newState with
                | Server.Internal _ ->
                    let oldState =
                        if stateQueue.IsEmpty ()
                        then UIState.State.Default
                        else stateQueue.Dequeue () |> Server.scopeToState
                        
                    uiServer.BroadcastState { oldState with SharedState = newState |> Server.scopeToState }
                    stateQueue.Enqueue ({ oldState with SharedState = newState |> Server.scopeToState } |> Server.Internal)
                
                | Server.Remote state ->
                    rabbitExchange.Post "" (SharedState.ClientStateUpdate state)
            | _ -> ()
        })
        
        use rabbitBus = RabbitQueue.createBus configToml.RabbitAddress "root" "root"
        let rabbitExchange = RabbitQueue.Exchange rabbitBus
        
        let rabbitHandlerAsync message __exchange = async {
            match message with
            | SharedState.StateUpdate state ->
                Log.Debug ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)
                
                stateBroadcastQueue.Enqueue (Server.Internal state)

            | _ -> ()
        }
        
        rabbitExchange.RegisterConsumer [ "#" ] rabbitHandlerAsync
        
        
        let stateQueue = SafeQueue.SafeQueue<Server.StateScope<UIState.State>> (fun _ newState -> async {
            match newState with
            | Server.Remote newState -> stateBroadcastQueue.Enqueue (Server.Remote newState.SharedState)
            | _ -> ()
        })
        
        _rabbitExchange <- Some rabbitExchange
        _stateQueue <- Some stateQueue
        
        let __initialState = stateQueue.Dequeue ()
        
        
        let onOffset offset =
            let state = stateQueue.Dequeue () |> Server.scopeToState
            
            let newTimeSyncMap =
                state.TimeSyncMap
                |> TimeSync.saveOffset offset
            
            { state with TimeSyncMap = newTimeSyncMap }
            |> Server.Internal
            |> stateQueue.Enqueue
            
        TimeSync.Client.start rabbitBus onOffset
        
        
        do! uiServer.HangAsync stateQueue
    }

    [<EntryPoint>]
    let main _ =
        fun () ->
            startAsync |> Async.RunSynchronously
        |> Startup.withLogging false

