namespace MechaHaze.CoreCLR

open MathNet.Numerics
open MechaHaze.Shared
open MechaHaze.CoreCLR.Core
open Serilog
open System
open MechaHaze.IO


module TimeSync =
    let withOffset (offset: int64) (timeSync: SharedState.TimeSync) =

        // Test performance seq/list
        let offsets =
            timeSync.Offsets
            |> Seq.rev
            |> Seq.truncate 50
            |> Seq.append [
                (float DateTime.UtcNow.Ticks, float offset)
                |> SharedState.TimeSyncOffset
               ]
            |> Seq.rev
            |> Seq.toArray

        let x, y =
            offsets
            |> Array.map SharedState.ofTimeSyncOffset
            |> Array.unzip

        let y' =
            if x.Length < 2 then
                y
            else
                x |> Array.map (Fit.lineFunc x y)

        (*
    let print =
    sprintf "Scoring: R2=%.2f R=%.2f PSE=%.2f SE=%.2f SAD=%.2f SSD=%.2f MAE=%.2f MSE=%.2f"
    (GoodnessOfFit.RSquared (y', y))
    (GoodnessOfFit.R (y', y))
    (GoodnessOfFit.PopulationStandardError (y', y))
    (GoodnessOfFit.StandardError (y', y, 2))
    (Distance.SAD (y', y))
    (Distance.SSD (y', y))
    (Distance.MAE (y', y))
    (Distance.MSE (y', y))
        *)

        let stableOffsets =
            Array.zip x y'
            |> Array.map SharedState.TimeSyncOffset

        let newSelfTimeSync =
            { SharedState.TimeSync.Default with
                Offsets = offsets
                StableOffsets = stableOffsets
            }

        newSelfTimeSync







    type Message =
        | Init of T1: int64
        | Align of processId: SharedState.ProcessId * T1: int64 * T2: int64
        | Finish of T1: int64 * T2: int64 * T3: int64


    let processId = SharedState.ProcessId (Random().Next() |> string)


    module Server =
        let handlerAsync message (exchange: RabbitQueue.Exchange<_>) =
            async {
                match message with
                | Align (clientReqId, T1, T2) ->
                    Finish (T1, T2, DateTime.UtcNow.Ticks)
                    |> exchange.Post ($"server.finish.{clientReqId}")
                | _ -> ()
            }

        let start bus =
            let exchange = RabbitQueue.Exchange bus

            exchange.RegisterConsumer
                [
                    "client.*"
                ]
                handlerAsync

            let timerEventAsync =
                async {
                    Init DateTime.UtcNow.Ticks
                    |> exchange.Post "server.init"
                }

            timerEventAsync
            |> Timer.hangAsync 1000
            |> Async.Start

    module Client =
        let handlerAsync (onOffset: int64 -> unit) message (exchange: RabbitQueue.Exchange<_>) =
            async {
                match message with
                | Init T1 ->
                    Align (processId, T1, DateTime.UtcNow.Ticks)
                    |> exchange.Post "client.align"

                | Finish (T1, T2, T3) ->
                    let offset = (T2 - T1 - T3 + T2) / 2L

                    Log.Debug ("Stabilized Time Sync. Offset: {A}", offset)

                    onOffset offset
                | _ -> ()
            }

        let start bus (onOffset: int64 -> unit) =
            let exchange = RabbitQueue.Exchange bus

            let bindingKeys =
                [
                    "server.*"
                    $"server.*.{processId}"
                ]

            exchange.RegisterConsumer bindingKeys (handlerAsync onOffset)


    let getOffset (timeSyncMap: SharedState.TimeSyncMap) =
        timeSyncMap
        |> SharedState.ofTimeSyncMap
        |> Map.tryFind processId
        |> function
        | None -> 0.
        | Some timeSync ->
            timeSync.StableOffsets
            |> Array.tryItem (timeSync.StableOffsets.Length / 2)
            |> function
            | None -> 0.
            | Some offset -> offset |> SharedState.ofTimeSyncOffset |> snd


    let saveOffset offset timeSyncMap =
        let timeSyncMap = timeSyncMap |> SharedState.ofTimeSyncMap

        let timeSync =
            timeSyncMap
            |> Map.tryFind processId
            |> Option.defaultValue SharedState.TimeSync.Default
            |> withOffset offset

        timeSyncMap
        |> Map.add processId timeSync
        |> SharedState.TimeSyncMap
