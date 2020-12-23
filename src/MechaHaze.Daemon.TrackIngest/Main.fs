namespace MechaHaze.Daemon.TrackIngest

open MechaHaze.CoreCLR
open Serilog
open MechaHaze.Daemon.TrackIngest
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
