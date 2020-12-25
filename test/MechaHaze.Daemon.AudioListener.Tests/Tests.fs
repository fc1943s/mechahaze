namespace MechaHaze.Daemon.AudioListener

open System
open Expecto
open Expecto.Flip
open MechaHaze.Shared.Core
open MechaHaze.Shared


module Tests =
    let tests =
        testList
            "Tests"
            [

                testList
                    "AudioRecorder"
                    [

                        test "AudioRecorder" {
                            let testSample (sample: LocalQueue.Sample) =
                                (sample.Timestamp, DateTime.UtcNow.Ticks)
                                |> Expect.isLessThan ""

                                (sample.Buffer.LongLength, Audio.averageSampleByteLength)
                                |> Expect.isGreaterThan ""


                            AudioRecorder.recordIoAsync
                            |> Async.RunSynchronously
                            |> function
                            | Ok sample ->
                                testSample sample

                                TrackMatcher.querySampleIoAsync sample
                                |> Async.RunSynchronously
                                |> Expect.equal "" SharedState.Track.Default

                            | Error ex -> raise ex
                        }
                    ]

                testList
                    "TrackMatcher"
                    [

                        let createTest () =
                            let io = TrackMatcher.Stabilizer.Io.create ()
                            { io with Fetch = io.Fetch }

                        test "Stabilizer" {
                            let empty = { SharedState.Track.Default with Id = "" }
                            let trackA = { SharedState.Track.Default with Id = "a" }
                            let trackB = { SharedState.Track.Default with Id = "b" }

                            let io = createTest ()

                            let test (oldTrack, newTrack, expected, expectedCacheLength) =
                                TrackMatcher.Stabilizer.stabilizeTrack oldTrack newTrack
                                |> Io.run io
                                |> fst
                                |> Expect.equal "" expected

                                io.Length ()
                                |> Expect.equal "" expectedCacheLength

                            ({ empty with Position = 0. }, { trackA with Position = 0. }, None, 1)
                            |> test

                            ({ empty with Position = 0. }, { trackA with Position = 0. }, None, 2)
                            |> test

                            ({ empty with Position = 0. },
                             { trackA with Position = 0. },
                             Some { trackA with Position = 0. },
                             0)
                            |> test

                            ({ trackA with Position = 0. }, { trackB with Position = 0. }, None, 1)
                            |> test

                            ({ trackA with Position = 0. }, { empty with Position = 0. }, None, 2)
                            |> test

                            ({ trackA with Position = 0. },
                             { trackB with Position = 0. },
                             Some { trackB with Position = 0. },
                             0)
                            |> test

                            ({ trackB with Position = 0. }, { empty with Position = 0. }, None, 1)
                            |> test

                            ({ trackB with Position = 0. }, { trackA with Position = 0. }, None, 2)
                            |> test

                            ({ trackB with Position = 0. },
                             { trackA with Position = 0. },
                             Some { trackA with Position = 0. },
                             0)
                            |> test

                            ({ trackA with Position = 0. }, { empty with Position = 0. }, None, 1)
                            |> test

                            ({ trackA with Position = 0. },
                             { trackA with Position = 1. },
                             Some { trackA with Position = 1. },
                             0)
                            |> test

                            ({ trackA with Position = 1. }, { empty with Position = 0. }, None, 1)
                            |> test

                            ({ trackA with Position = 1. }, { trackA with Position = 1. }, None, 0)
                            |> test

                            ({ trackA with Position = 1. }, { empty with Position = 0. }, None, 1)
                            |> test

                            ({ trackA with Position = 1. }, { empty with Position = 0. }, None, 2)
                            |> test

                            ({ trackA with Position = 1. },
                             { empty with Position = 1. },
                             Some { empty with Position = 1. },
                             0)
                            |> test

                            ({ empty with Position = 1. }, { trackA with Position = 1. }, None, 1)
                            |> test

                            ({ empty with Position = 1. }, { trackA with Position = 1. }, None, 2)
                            |> test

                            ({ trackA with Position = 1. },
                             { trackA with Position = 2. },
                             Some { trackA with Position = 2. },
                             0)
                            |> test

                            ({ trackA with Position = 2. },
                             { trackA with Position = 3. },
                             Some { trackA with Position = 3. },
                             0)
                            |> test

                            ({ trackA with Position = 3. },
                             { trackA with Position = 4. },
                             Some { trackA with Position = 4. },
                             0)
                            |> test

                            ({ trackA with Position = 4. },
                             { trackA with Position = 5. },
                             Some { trackA with Position = 5. },
                             0)
                            |> test

                            ({ trackA with Position = 5. },
                             { trackA with Position = 6. },
                             Some { trackA with Position = 6. },
                             0)
                            |> test

                            ({ trackA with Position = 6. }, { trackA with Position = 6. }, None, 0)
                            |> test

                            ({ trackA with Position = 6. },
                             { trackA with Position = 7. },
                             Some { trackA with Position = 7. },
                             0)
                            |> test
                        }
                    ]
            ]
