namespace MechaHaze.Daemon.AudioListener

open System
open MechaHaze.Shared
open MechaHaze.CoreCLR
open MechaHaze.Shared.Bindings
open MechaHaze.Shared.SharedState
open Serilog
open MechaHaze.Shared.Core
open System.IO
open MechaHaze.CoreCLR.Core
open FSharp.Control.Tasks
open Thoth.Json.Net


module StatePersistence =
    type StateUri = StateUri of uri: Uri

    let stateUriMemoizedLazy =
        fun () ->
            (SharedConfig.pathsLazyIo ()).dbState
            </> "main.json"
            |> Uri
            |> StateUri
        |> Core.memoizeLazy

    module Json =
        module MatchDebugInfo =
            let encoder matchDebugInfo =
                Encode.object [
                    nameof matchDebugInfo.Name, Encode.string matchDebugInfo.Name
                    nameof matchDebugInfo.TrackStartsAt, Encode.float matchDebugInfo.TrackStartsAt

                    nameof matchDebugInfo.TotalTracksAnalyzed, Encode.int matchDebugInfo.TotalTracksAnalyzed

                    nameof matchDebugInfo.TotalFingerprintsAnalyzed, Encode.int matchDebugInfo.TotalFingerprintsAnalyzed

                    nameof matchDebugInfo.Score, Encode.float matchDebugInfo.Score
                    nameof matchDebugInfo.Confidence, Encode.float matchDebugInfo.Confidence

                    nameof matchDebugInfo.ResultEntriesLength, Encode.int matchDebugInfo.ResultEntriesLength

                    nameof matchDebugInfo.AvgScoreAcrossBestPath, Encode.float matchDebugInfo.AvgScoreAcrossBestPath
                ]

            let decoder: Decoder<MatchDebugInfo> =
                Decode.object (fun get ->
                    {
                        Name = get.Required.Field (nameof MatchDebugInfo.Default.Name) Decode.string
                        TrackStartsAt = get.Required.Field (nameof MatchDebugInfo.Default.TrackStartsAt) Decode.float
                        TotalTracksAnalyzed =
                            get.Required.Field (nameof MatchDebugInfo.Default.TotalTracksAnalyzed) Decode.int
                        TotalFingerprintsAnalyzed =
                            get.Required.Field (nameof MatchDebugInfo.Default.TotalFingerprintsAnalyzed) Decode.int
                        Score = get.Required.Field (nameof MatchDebugInfo.Default.Score) Decode.float
                        Confidence = get.Required.Field (nameof MatchDebugInfo.Default.Confidence) Decode.float
                        ResultEntriesLength =
                            get.Required.Field (nameof MatchDebugInfo.Default.ResultEntriesLength) Decode.int
                        AvgScoreAcrossBestPath =
                            get.Required.Field (nameof MatchDebugInfo.Default.AvgScoreAcrossBestPath) Decode.float
                    })

        module Track =
            let encoder track =
                Encode.object [
                    nameof track.Id, Encode.string (track.Id |> ofTrackId)
                    nameof track.Position, Encode.float track.Position
                    nameof track.DurationSeconds, Encode.float track.DurationSeconds
                    nameof track.DebugInfo, MatchDebugInfo.encoder track.DebugInfo
                    nameof track.Locked, Encode.bool track.Locked
                    nameof track.Offset, Encode.float track.Offset
                    nameof track.Timestamp, Encode.int64 track.Timestamp
                ]

            let decoder: Decoder<Track> =
                Decode.object (fun get ->
                    {
                        Id = TrackId (get.Required.Field (nameof Track.Default.Id) Decode.string)
                        Position = get.Required.Field (nameof Track.Default.Position) Decode.float
                        DurationSeconds = get.Required.Field (nameof Track.Default.DurationSeconds) Decode.float
                        DebugInfo = get.Required.Field (nameof Track.Default.DebugInfo) MatchDebugInfo.decoder
                        Locked = get.Required.Field (nameof Track.Default.Locked) Decode.bool
                        Offset = get.Required.Field (nameof Track.Default.Offset) Decode.float
                        Timestamp = get.Required.Field (nameof Track.Default.Timestamp) Decode.int64
                    })


        module Binding =
            let encoder (Binding (BindingSourceId bindingSourceId, BindingDestId bindingDestId)) =
                Encode.object [
                    nameof BindingSourceId, Encode.string bindingSourceId
                    nameof BindingDestId, Encode.string bindingDestId
                ]

            let decoder: Decoder<Binding> =
                Decode.object (fun get ->
                    Binding
                        (BindingSourceId (get.Required.Field (nameof BindingSourceId) Decode.string),
                         BindingDestId (get.Required.Field (nameof BindingDestId) Decode.string)))

        module Preset =
            let encoder preset =
                Encode.object [
                    nameof preset.PresetId, Encode.string (preset.PresetId |> ofPresetId)
                    nameof preset.Bindings,
                    preset.Bindings
                    |> List.map Binding.encoder
                    |> Encode.list
                ]

            let decoder: Decoder<Preset> =
                Decode.object (fun get ->
                    {
                        PresetId = PresetId (get.Required.Field (nameof Preset.Default.PresetId) Decode.string)
                        Bindings = get.Required.Field (nameof Preset.Default.Bindings) (Decode.list Binding.decoder)
                    })

        module SharedState =
            let encoder (state: SharedState.SharedState) =
                Encode.object [
                    nameof state.Track, Track.encoder state.Track
                    nameof state.Debug, Encode.bool state.Debug
                    nameof state.AutoLock, Encode.bool state.AutoLock
                    nameof state.RecordingMode, Encode.bool state.RecordingMode

                    nameof state.ActivePresetId, (Encode.option (ofPresetId >> Encode.string)) (state.ActivePresetId)

                    nameof state.PresetList,
                    state.PresetList
                    |> List.map Preset.encoder
                    |> Encode.list
                ]

            let decoder: Decoder<SharedState.SharedState> =
                Decode.object (fun get ->
                    {
                        Track = get.Required.Field (nameof Track) Track.decoder
                        Debug = get.Required.Field (nameof SharedState.Default.Debug) Decode.bool
                        AutoLock = get.Required.Field (nameof SharedState.Default.AutoLock) Decode.bool
                        RecordingMode = get.Required.Field (nameof SharedState.Default.RecordingMode) Decode.bool
                        PresetList =
                            get.Required.Field (nameof SharedState.Default.PresetList) (Decode.list Preset.decoder)
                        ActivePresetId =
                            get.Optional.Field (nameof SharedState.Default.ActivePresetId) Decode.string
                            |> Option.map PresetId
                    })


        let serialize value = Encode.toString 4 (SharedState.encoder value)

        let inline deserialize<'a> =
            Json.deserializeWith (fun json ->
                Decode.fromString SharedState.decoder json
                |> Result.mapError exn)


    let read<'a> (StateUri uri) =
        task {
            let! json = File.ReadAllTextAsync uri.AbsolutePath

            return
                Json.deserialize<SharedState.SharedState> json
                |> Result.mapError (fun ex ->
                    File.Copy (uri.AbsolutePath, $"{uri.AbsolutePath}.{Core.getTimestamp DateTime.Now}.error.json")
                    ex)
        }


    let write (StateUri uri) (newState: SharedState.SharedState) =
        task {
            try
                let json = Json.serialize newState

                uri.AbsolutePath
                |> Path.GetDirectoryName
                |> Directory.CreateDirectory
                |> ignore

                do! File.WriteAllTextAsync (uri.AbsolutePath, json)

                File.Copy (uri.AbsolutePath, $"{uri.AbsolutePath}.{Core.getTimestamp DateTime.Now}.event.json")

                return Ok ()
            with ex -> return Error ex
        }
