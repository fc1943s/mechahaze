namespace MechaHaze.FeatureDispatcher.Daemon

open MechaHaze.FeatureDispatcher.Daemon
open MechaHaze.Shared
open MechaHaze.Shared.CoreCLR
open Serilog
open Suigetsu.Bus
open Suigetsu.Core
open Suigetsu.CoreCLR

module Program =
    let startAsync = async {
        let configToml = SharedConfig.loadTomlIo ()
        
        use rabbitBus = RabbitQueue.createBus configToml.RabbitAddress "root" "root"
        let rabbitExchange = RabbitQueue.Exchange rabbitBus
        
        let rabbitHandlerAsync message __exchange = async {
            match message with
            | SharedState.StateUpdate state ->
                Log.Debug ("State received. Track: {A}; Position: {B}", state.Track.Id, state.Track.Position)
                
                OscDispatcher.updateState state

            | _ -> ()
        }
        
        rabbitExchange.RegisterConsumer [ "#" ] rabbitHandlerAsync
        

        let timeSyncMapState = SafeQueue.SafeQueue<SharedState.TimeSyncMap> (fun _ _ -> async {()})
        timeSyncMapState.Enqueue (Map.empty |> SharedState.TimeSyncMap)

        let onOffset offset =
            let newTimeSyncMap =
                timeSyncMapState.Dequeue ()
                |> TimeSync.saveOffset offset
                
            TimeSync.getOffset newTimeSyncMap
            |> int64
            |> OscDispatcher.updateOffset
                
            newTimeSyncMap
            |> timeSyncMapState.Enqueue
            
        TimeSync.Client.start rabbitBus onOffset
        
        let stateQueue = SafeQueue.SafeQueue<SharedState.SharedState> (fun oldState newState -> async {
            Log.Debug ("Sending state update.\nOld: {OldState} \nNew: {NewState}", oldState, newState)
            
            OscDispatcher.updateState newState
            
            rabbitExchange.Post "" (SharedState.ClientStateUpdate newState)
        })
        
        do! OscDispatcher.hangAsync stateQueue
    }

    [<EntryPoint>]
    let main _ =
        Logging.addLoggingSink Logging.consoleSink false

        try
            try
                startAsync |> Async.RunSynchronously

                Log.Information ("Program end")
                0
            with ex ->
                Log.Error (ex, "Program error")
                1
        finally
            Log.CloseAndFlush ()
