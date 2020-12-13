namespace MechaHaze.AudioListener.Daemon

open FSharp.Control
open System
open NAudio.Wave
open NAudio.CoreAudioApi
open System.IO
open System.Threading
open Serilog
open Suigetsu.Core
open NAudio.Wave.SampleProviders

module SilentSinePlayer =
    let private hangIoAsync =
        async {
            try
                Log.Debug ("Starting SilentSinePlayer")
                let sine = SignalGenerator (Gain = 0., Frequency = 20., Type = SignalGeneratorType.Sin)

                use waveOutEvent = new WaveOutEvent()
                waveOutEvent.Init (sine, true)
                waveOutEvent.Play ()

                while waveOutEvent.PlaybackState = PlaybackState.Playing do
                    do! Async.Sleep 1000

            with ex -> Log.Error (ex, "Error running SilentSinePlayer")
        }

    let startMemoizedIo =
        fun () -> hangIoAsync |> Async.Start
        |> Core.memoizeLazy

module AudioRecorder =
    let private unsafeRecordIoAsync =
        fun (succ, _, _) ->
            let onError ex = exn ("Error recording audio", ex) |> Error |> succ

            try
                let timestamp = DateTime.UtcNow.Ticks

                use capture = new WasapiLoopbackCapture()
                use stream = new MemoryStream()

                let dataAvailable (args: WaveInEventArgs) =
                    try
                        stream.Write (args.Buffer, 0, args.BytesRecorded)

                        if stream.Position > Audio.averageSampleByteLength then
                            let buffer = stream.ToArray ()

                            let message: LocalQueue.Sample = { Buffer = buffer; Timestamp = timestamp }

                            Ok message |> succ

                            capture.StopRecording ()
                    with ex -> onError ex

                let recordingStopped _ = ()

                capture.DataAvailable.Add dataAvailable
                capture.RecordingStopped.Add recordingStopped
                capture.StartRecording ()

                while capture.CaptureState <> CaptureState.Stopped do
                    Thread.Sleep 100
            with ex -> onError ex
        |> Async.FromContinuations

    let recordIoAsync =
        async {
            SilentSinePlayer.startMemoizedIo ()
            return! unsafeRecordIoAsync
        }
