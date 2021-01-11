namespace MechaHaze.Daemon.FeatureDispatcher

open System
open System.Collections.Concurrent
open System.IO
open System.Net.Sockets
open System.Threading
open CoreOSC
open CoreOSC.Types
open MechaHaze.CoreCLR.Core
open MechaHaze.Shared
open FSharp.Control
open MechaHaze.CoreCLR
open MechaHaze.Shared.Bindings
open MechaHaze.Shared.Core
open Serilog
open CoreOSC.IO


module OscDispatcher =
    let mutable private _state: SharedState.SharedState option = None
    let mutable private _preset: (seq<(string * string) * (string * string)>) option = None
    let mutable private _offset = 0L
    let private lockState = Object ()

    let private peaksLevelsDict = ConcurrentDictionary<string, Waveform.WaveformModel> ()
    let private peaksPitchDict = ConcurrentDictionary<string, Waveform.WaveformModel> ()

    let updateOffset offset = _offset <- offset

    let updateState state =
        lock lockState (fun _ ->
            let oldState = _state

            _state <- Some state

            _preset <-
                state.PresetList
                |> List.tryFind (fun preset -> Some preset.PresetId = state.ActivePresetId)
                |> Option.map (fun preset ->
                    splitPreset preset
                    |> Seq.filter (fun ((fullSource, _), _) -> fullSource <> ""))

            if oldState.IsNone
               || state.Track.Id <> SharedState.Track.Default.Id
                  && oldState.Value.Track.Id <> state.Track.Id then
                peaksLevelsDict.Clear ()
                peaksPitchDict.Clear ()

                if state.Track.Id <> SharedState.Track.Default.Id then
                    layers
                    |> List.iter (fun (layer, _) ->
                        async {
                            let peaksLevelsPath, peaksPitchPath =
                                (sources.Levels, sources.Pitch)
                                |> Tuple2.map (fun format ->
                                    let (SharedState.TrackId trackId) = state.Track.Id

                                    (SharedConfig.pathsLazyIo ()).dbTracks
                                    </> trackId
                                    </> $"{trackId}.{layer}.peaks.{format}.dat")

                            if File.Exists peaksLevelsPath then
                                let peaksLevels = Waveform.readPeaks peaksLevelsPath

                                peaksLevelsDict.AddOrUpdate (layer, peaksLevels, (fun _ _ -> peaksLevels))
                                |> ignore
                            else
                                Log.Error ("Peaks levels not found for track {Track}", state.Track.Id)

                            if File.Exists peaksPitchPath then
                                let peaksPitch = Waveform.readPeaks peaksPitchPath

                                peaksPitchDict.AddOrUpdate (layer, peaksPitch, (fun _ _ -> peaksPitch))
                                |> ignore
                            else
                                Log.Error ("Peaks pitch not found for track {Track}", state.Track.Id)
                        }
                        |> Async.Start))

    let udpWrapperAsync fn =
        async {
            let socketExceptionHandler (ex: SocketException) =
                if ex.ErrorCode = 10054 then
                    Log.Debug ("Port closed.")
                else
                    Log.Error (ex, "Unknown Socket Error")

            try
                let! result = fn
                return Some result
            with
            | :? AggregateException as ex when (ex.InnerException :? SocketException) ->
                socketExceptionHandler (ex.InnerException :?> SocketException)
                return None

            | :? SocketException as ex ->
                socketExceptionHandler ex
                return None

            | :? TimeoutException ->
                Log.Error ("Socket timeout")
                return None

            | ex ->
                Log.Error (ex, "Unhandled exception")
                return None
        }


    let mutable private _didInitReceived = false
    let private initReceiverLock = Object ()

    let hangAsync (stateQueue: SafeQueue.SafeQueue<SharedState.SharedState>) =
        async {
            Log.Debug ("Starting osc dispatcher")

            let bytesConverter = BytesConverter ()
            let bundleConverter = OscBundleConverter ()

            use resolumeClient = new UdpClient("127.0.0.1", 7000)
            use magicClient = new UdpClient("127.0.0.1", 8000)

            use receiveClient = new UdpClient(7001)


            let messageResponseQueue = ConcurrentDictionary<string, ConcurrentBag<string * obj -> unit>> ()

            let initReceiver () =
                lock initReceiverLock (fun _ ->
                    if not _didInitReceived then
                        _didInitReceived <- true

                        async {
                            while true do
                                do! async {
                                        let! response =
                                            receiveClient.ReceiveMessageAsync ()
                                            |> Async.AwaitTask

                                        let hasEvents, events = messageResponseQueue.TryGetValue response.Address.Value

                                        if hasEvents then
                                            let success, event = events.TryTake ()

                                            if success then
                                                let response =
                                                    response.Address.Value,
                                                    response.Arguments |> Seq.cast<obj> |> Seq.toArray

                                                event response

                                    }
                                    |> udpWrapperAsync
                                    |> Async.Ignore

                        }
                        |> Async.Start)

            let sendMessageAsync (client: UdpClient) feature (value: obj) (responseEvent: (string * obj -> unit) option) =
                async {
                    let message = OscMessage (Address feature, seq { value })

                    match responseEvent with
                    | None -> ()
                    | Some responseEvent ->
                        initReceiver ()

                        let events = messageResponseQueue.GetOrAdd (feature, (fun _ -> ConcurrentBag<_> ()))
                        events.Add responseEvent

                    do! async { do! client.SendMessageAsync message |> Async.AwaitTask }
                        |> udpWrapperAsync
                        |> Async.Ignore
                }


            Some ()
            |> Option.iter (fun _ ->
                async {
                    while true do
                        do! sendMessageAsync
                                resolumeClient
                                "/composition/layers/1/master"
                                "?"
                                (Some (fun response -> Log.Debug ("Periodic check response: {@Response}", response)))

                        do! Async.Sleep 7000
                }
                |> Async.Start)

            while true do
                let layers =
                    lock lockState (fun () ->
                        match _state with
                        | Some state when state.Track.Position > 0. ->

                            let diff =
                                (float
                                    (DateTime.UtcNow.Ticks
                                     - state.Track.Timestamp
                                     - _offset)
                                 / 10000.
                                 / 1000.)
                                - state.Track.Offset

                            layers
                            |> List.toArray
                            |> Array.Parallel.collect (fun (layer, _) ->
                                let peaksSuccess, peaks = peaksLevelsDict.TryGetValue layer
                                let peaksPitchSuccess, peaksPitch = peaksPitchDict.TryGetValue layer

                                let levelAverage, panL, panR, pitch =
                                    if peaksSuccess then
                                        let peaksIndex =
                                            let rawIndex =
                                                float peaks.Header.SampleRate
                                                * (state.Track.Position + diff)

                                            let stepSize = float peaks.Header.SamplesPerPixel

                                            let index = rawIndex / stepSize |> int

                                            if index >= peaks.ChannelL.Length then
                                                if state.Track.Locked then
                                                    Log.Debug ("Unlocking track after end of streaming")

                                                    stateQueue.Enqueue
                                                        { state with
                                                            Track = { state.Track with Locked = false }
                                                        }

                                                0
                                            else
                                                index

                                        let L = peaks.ChannelL.[peaksIndex]
                                        let R = peaks.ChannelR.[peaksIndex]

                                        let pitch =
                                            if peaksPitchSuccess then
                                                peaksPitch.ChannelL.[peaksIndex] * 3.
                                            else
                                                0.

                                        let levelAverage = (L + R) / 2.

                                        let levelAverage =
                                            if layer <> "all" then
                                                levelAverage * 1.3
                                            else
                                                levelAverage

                                        let panL = max 0. (L - R)
                                        let panR = max 0. (R - L)


                                        levelAverage, panL, panR, pitch
                                    else
                                        0., 0., 0., 0.

                                [|
                                    sources.Levels + string separator + layer, levelAverage
                                    sources.Pitch + string separator + layer, pitch
                                    sources.PanL + string separator + layer, panL
                                    sources.PanR + string separator + layer, panR
                                |])
                            |> Map.ofArray
                            |> Some
                        | _ -> None)

                match _preset, layers with
                | Some preset, Some layers ->
                    let messages =
                        preset
                        |> Seq.choose (fun ((fullSource, _), (fullDest, dest)) ->
                            layers
                            |> Map.tryFind fullSource
                            |> function
                            | Some value ->
                                let value =
                                    if dest = "resolume|/composition/tempocontroller/tempo" then
                                        value / 2.
                                    else
                                        value

                                let client =
                                    if fullDest.StartsWith destinations.Resolume then
                                        Some resolumeClient
                                    elif fullDest.StartsWith destinations.Magic then
                                        Some magicClient
                                    else
                                        Log.Error ("Invalid binding prefix")
                                        None

                                Some (dest, value, client)

                            | None ->
                                Log.Warning ("Binding registed but no value found: {Source}", fullSource)
                                None)

                    let bundling = false

                    if not bundling then
                        do! messages
                            |> Seq.choose (fun (feature, value, client) ->
                                client
                                |> Option.map (fun client -> sendMessageAsync client feature value None))
                            |> Async.Parallel
                            |> Async.Ignore
                    else
                        let messages =
                            messages
                            |> Seq.map (fun (feature, value, client) ->
                                client, OscMessage (Address feature, seq { value }))
                            |> Seq.groupBy fst

                        let now = Timetag.FromDateTime DateTime.UtcNow

                        do! messages
                            |> Seq.map (fun (client, messages) ->
                                async {
                                    match client with
                                    | Some client ->
                                        let bytes =
                                            OscBundle (now, messages |> Seq.map snd)
                                            |> bundleConverter.Serialize
                                            |> bytesConverter.Deserialize
                                            |> snd
                                            |> Seq.toArray

                                        let! res =
                                            client.SendAsync (bytes, bytes.Length)
                                            |> Async.AwaitTask

                                        if res <= 0 then
                                            Log.Debug ("<0 osc response.")

                                    | None -> ()
                                })
                            |> Async.Parallel
                            |> Async.Ignore

                | _ -> ()

                match _state with
                | None -> 20
                | Some { Track = track } when not track.Locked -> 20
                | _ -> 1
                |> Thread.Sleep

        }
