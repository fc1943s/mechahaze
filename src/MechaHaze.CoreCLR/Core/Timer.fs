namespace MechaHaze.CoreCLR.Core

open System.Threading.Tasks
open System.Diagnostics
open Serilog
open System.Threading
open FSharp.Control.Tasks

module Timer =
    let hangAsync interval (timerEventAsync: unit -> Task) =
        task {
            let mutable _timer: Timer = null

            _timer <-
                new Timer(TimerCallback (fun _ ->
                              let stopwatch = Stopwatch ()
                              stopwatch.Start ()

                              try
                                  (timerEventAsync ()).GetAwaiter().GetResult()
                              with ex -> Log.Error (ex, "Error on timer event")

                              //  Log.Debug ("DIFFERENCE {DIF}", interval - int stopwatch.ElapsedMilliseconds)

                              if not
                                  (_timer.Change
                                      (max 0 (interval - int stopwatch.ElapsedMilliseconds), Timeout.Infinite)) then
                                  Log.Error
                                      ("Error changing timer interval {A} {B}", interval, stopwatch.ElapsedMilliseconds)

                              ),
                          null,
                          interval,
                          Timeout.Infinite)

            while true do
                do! Task.Delay 1000

            _timer.Dispose ()
        }
