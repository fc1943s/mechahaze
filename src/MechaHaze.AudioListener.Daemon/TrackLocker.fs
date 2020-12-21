namespace MechaHaze.AudioListener.Daemon

open MechaHaze.Shared
open Serilog
open System.Collections.Concurrent


module TrackLocker =
    let noiseCache = ConcurrentStack<float * float> ()
    let mutable _offsetCeiling = 0.

    let reset () =
        noiseCache.Clear ()
        _offsetCeiling <- 0.

    let lockTrack (state: SharedState.SharedState) (track: SharedState.Track) =
        let posDiff =
            SharedState.sampleIntervalSeconds
            - (track.Position - state.Track.Position)

        let ticksDiff =
            SharedState.sampleIntervalSeconds
            - (float (track.Timestamp - state.Track.Timestamp)
               / 10000.
               / 1000.)

        Log.Verbose ("PosDiff: {PosDiff}. TicksDiff: {TicksDiff}", $"%.4f{posDiff}", $"%.4f{ticksDiff}") (*** // ***)

        noiseCache.Push (posDiff, ticksDiff)

        let recentNoise =
            noiseCache
            |> Seq.truncate SharedState.lockingNoiseCount
            |> Seq.toList

        let sumPos, sumTicks = recentNoise |> List.unzip |> Tuple2.map List.sum

        let offset = (-sumPos + posDiff) / 2.
        let offset2 = (-posDiff + ticksDiff) / 2.

        Log.Verbose
            ("count: {Count}. Offset: {Offset} SUM pos {SumPos}. Sum ticks {SumTicks}. Noise: {Noise}",
             noiseCache.Count,
             $"%.4f{offset}",
             $"%.4f{sumPos}",
             $"%.4f{sumTicks}",
             recentNoise
             |> List.map (fun (sum, ticks) -> ($"%.4f{sum}", $"%.4f{ticks}"))) (*** // ***)

        Log.Verbose ("Offset2: {Offset}; Ceiling: {Ceiling}", $"%.4f{offset2}", _offsetCeiling) (*** // ***)

        let limit = 0.2

        if recentNoise.Length
           <> SharedState.lockingNoiseCount
           || not (sumPos >< (-limit, limit)) then //  && (_offsetCeiling = 0. || abs offset < _offsetCeiling)
            state.Track.Locked, 0.
        else
            if state.AutoLock then
                Log.Information ("Match inside limits. Locking track.")
            else
                Log.Verbose ("Match inside limits. Setting new offset ceiling.") (*** // ***)
                _offsetCeiling <- abs offset

            state.AutoLock, offset
//        state.AutoLock, 0.
