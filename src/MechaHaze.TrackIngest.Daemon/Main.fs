﻿namespace MechaHaze.TrackIngest.Daemon

open Serilog
open MechaHaze.TrackIngest.Daemon
open NAudio.MediaFoundation


module Main =

    [<EntryPoint>]
    let main _ =
        Logging.addLoggingSink Logging.consoleSink true
        MediaFoundationApi.Startup ()

        try
            [
                RecJob.listenAsync
                RecJob.listenTracksHealthAsync
            ]
            |> Async.handleParallel
            |> Async.Parallel
            |> Async.Ignore
            |> Async.RunSynchronously

            Log.Information ("End")
            0
        with ex ->
            Log.Error (ex, "Program error")
            1
