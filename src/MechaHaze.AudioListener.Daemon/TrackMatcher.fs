namespace MechaHaze.AudioListener.Daemon

open System
open NAudio.Wave
open Serilog
open Suigetsu.Core
open System.Collections.Concurrent
open System.IO
open SoundFingerprinting.Builder
open MechaHaze.Shared
open MechaHaze.Shared.CoreCLR
open SoundFingerprinting.Audio.Bass
open SoundFingerprinting.Extensions.LMDB
open SoundFingerprinting.Query

module TrackMatcher =
    let private saveTempSampleIoAsync (sample: LocalQueue.Sample) =
        async {
            let outputFilePath =
                Path.Combine ((SharedConfig.pathsLazyIo ()).tempSamples, string (System.Random().Next()) + ".wav")

            use writer = new WaveFileWriter(outputFilePath, Audio.NAudio.waveFormat)

            do! writer.WriteAsync (sample.Buffer, 0, sample.Buffer.Length)
                |> Async.AwaitTask

            return outputFilePath
        }

    let private servicesLazyIo =
        fun () ->
            {|
                Audio = BassAudioService ()
                Model = new LMDBModelService((SharedConfig.pathsLazyIo ()).dbFingerprints)
            |}
        |> Core.memoizeLazy

    let private queryFingerprintIoAsync (path: string) =
        async {
            try
                return!
                    QueryCommandBuilder
                        .Instance
                        .BuildQueryCommand()
                        .From(path)
                        .UsingServices((servicesLazyIo ()).Model, (servicesLazyIo ()).Audio)
                        .Query(DateTime.UtcNow)
                    |> Async.AwaitTask
            with ex ->
                Log.Error (ex, "Error on LMDB Query")
                return QueryResult ([], QueryStats (0, 0, 0L, 0L))
        }

    let queryTrackDurationIo id =
        let path = Path.Combine ((SharedConfig.pathsLazyIo ()).dbTracks, id, id + ".mp3")

        if File.Exists path |> not then
            0.
        else
            use reader = new AudioFileReader(path)
            reader.TotalTime.TotalSeconds

    let private createDebugInfo (queryResult: QueryResult) =
        {
            SharedState.Name = sprintf "%s - %s" queryResult.BestMatch.Track.Artist queryResult.BestMatch.Track.Title
            SharedState.TrackStartsAt = queryResult.BestMatch.TrackStartsAt
            SharedState.TotalTracksAnalyzed = queryResult.Stats.TotalTracksAnalyzed
            SharedState.TotalFingerprintsAnalyzed = queryResult.Stats.TotalFingerprintsAnalyzed
            SharedState.Score = queryResult.BestMatch.Score
            SharedState.Confidence = queryResult.BestMatch.Confidence
            SharedState.ResultEntriesLength = Seq.length queryResult.ResultEntries
            SharedState.AvgScoreAcrossBestPath = queryResult.BestMatch.Coverage.AvgScoreAcrossBestPath
        }

    let querySampleIoAsync (sample: LocalQueue.Sample) =
        async {

            let! outputFilePath = saveTempSampleIoAsync sample

            let! queryResult = queryFingerprintIoAsync outputFilePath
            //  Log.Debug ("QueryResult: {@a}", queryResult)

            File.Delete outputFilePath

            if queryResult.ContainsMatches then
                let elapsed =
                    float (DateTime.UtcNow.Ticks - sample.Timestamp)
                    / 10000.
                    / 1000.

                if elapsed < SharedState.sampleLengthSeconds * 2. then
                    let id = queryResult.BestMatch.Track.Id
                    let position = queryResult.BestMatch.TrackMatchStartsAt

                    Log.Debug
                        (sprintf "Match Found. Id: {Id}. Position: {Position}. Elapsed: {Elapsed}",
                         id,
                         position,
                         elapsed)

                    return
                        {
                            SharedState.Id = id
                            SharedState.Position = position
                            SharedState.DurationSeconds = queryTrackDurationIo id
                            SharedState.Offset = elapsed
                            SharedState.Locked = false
                            SharedState.DebugInfo = createDebugInfo queryResult
                            SharedState.Timestamp = sample.Timestamp
                        }
                else
                    return SharedState.Track.Default
            else
                return SharedState.Track.Default
        }

    module Stabilizer =
        module Io =
            type State =
                {
                    Add: SharedState.Track -> unit
                    Clear: unit -> unit
                    Length: unit -> int
                    Fetch: int -> SharedState.Track list
                }

            let create () =
                let cache = ConcurrentStack<SharedState.Track> ()

                {
                    Add = cache.Push
                    Clear = cache.Clear
                    Length = fun () -> cache.Count
                    Fetch = fun expectedLength -> cache |> Seq.take expectedLength |> Seq.toList
                }


        let stabilizeTrack (oldTrack: SharedState.Track)
                           (newTrack: SharedState.Track)
                           : IoState<Io.State, SharedState.Track option> =
            Io.state {
                let! state = Io.getState

                state.Add newTrack

                Log.Debug ("CACHE LEN {A}", state.Length ())

                if oldTrack.Id = newTrack.Id then
                    state.Clear ()

                    let (|Valid|Invalid|) =
                        function
                        | _, { SharedState.Id = "" } -> Invalid
                        | { SharedState.Position = oldPosition }, { Position = newPosition } when oldPosition = newPosition ->
                            Invalid
                        | _ -> Valid

                    return
                        match oldTrack, newTrack with
                        | Valid -> Some newTrack
                        | Invalid -> None

                elif state.Length () >= SharedState.matchStabilityCount then
                    let lastMatchesIds =
                        state.Fetch SharedState.matchStabilityCount
                        |> List.map (fun x -> x.Id)

                    Log.Debug ("Last track matches: {Matches}", lastMatchesIds)

                    if lastMatchesIds |> List.forall ((<>) oldTrack.Id)
                       || lastMatchesIds |> List.forall ((=) newTrack.Id) then
                        state.Clear ()
                        return Some newTrack
                    else
                        return None
                else
                    return None
            }
