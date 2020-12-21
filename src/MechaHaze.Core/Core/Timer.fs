namespace MechaHaze.Core

open FSharp.Control
open System.Diagnostics
open Serilog
open System.Threading

module Timer =
    let hangAsync interval (timerEventAsync: Async<unit>) = async {
        let mutable _timer : Timer = null

        _timer <- new Timer (TimerCallback (fun _ ->
            let stopwatch = Stopwatch ()
            stopwatch.Start ()

            try
                timerEventAsync |> Async.RunSynchronously
            with ex ->
                Log.Error (ex, "Error on timer event")

        //  Log.Debug ("DIFFERENCE {DIF}", interval - int stopwatch.ElapsedMilliseconds)

            if not (_timer.Change (max 0 (interval - int stopwatch.ElapsedMilliseconds), Timeout.Infinite)) then
                Log.Error ("Error changing timer interval {A} {B}", interval, stopwatch.ElapsedMilliseconds)

        ), null, interval, Timeout.Infinite)

        while true do
            do! Async.Sleep 1000

        _timer.Dispose ()
    }
