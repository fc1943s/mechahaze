namespace MechaHaze.TrackIngest.Daemon

open MechaHaze.Shared

open MechaHaze.Shared.CoreCLR
open NAudio.Wave
open Serilog
open SoundFingerprinting.Audio.Bass
open SoundFingerprinting.Builder
open SoundFingerprinting.Data
open SoundFingerprinting.Extensions.LMDB
open Suigetsu.Core
open Suigetsu.CoreCLR
open System
open System.IO
open System.Linq

module RecJob =
    let private persistFingerprintAsync (path: string) id =
        async {
            Log.Debug ("Persisting fingerprint")

            let audioService = BassAudioService ()
            use modelService = new LMDBModelService((SharedConfig.pathsLazyIo ()).dbFingerprints)

            let trackData = modelService.ReadTrackById id

            if trackData = null then
                try
                    let! hashedFingerprints =
                        FingerprintCommandBuilder
                            .Instance
                            .BuildFingerprintCommand()
                            .From(path)
                            .UsingServices(audioService)
                            .Hash()
                        |> Async.AwaitTask

                    modelService.Insert (TrackInfo (id, id, id), hashedFingerprints)
                with ex -> exn ("Error inserting fingerprints", ex) |> raise
            else
                Log.Debug ("Track ID already found on fingerprint database")
        }

    let persistWaveformAsync sourcePath destPath =
        async {
            Log.Debug ("Persisting waveform")

            let! exitCode, _ =
                Runtime.executePowerShellAsync [
                    sprintf
                        """ %s -i "%s" -o "%s" -b 8 --split-channels """
                        (SharedConfig.pathsLazyIo ()).extAudiowaveformExe
                        sourcePath
                        destPath
                ]

            if exitCode <> 0 then
                failwith "Error persisting waveform"
        }

    let wavTo16 path =
        let path32 = path + ".32"
        File.Move (path, path32)

        do use reader = new AudioFileReader(path32)
           WaveFileWriter.CreateWaveFile16 (path, reader)

        File.Delete path32

    let waveFormat16 (waveFormat: WaveFormat) = WaveFormat (waveFormat.SampleRate, 16, waveFormat.Channels)

    let recombineAudio16 slices (combinedPath: string) =

        let buffer =
            slices
            |> Seq.map (fun path ->
                seq {
                    use reader = new AudioFileReader(path)

                    let buffer = Array.zeroCreate<byte> 4096

                    let mutable _read = 0

                    while (_read <- reader.Read (buffer, 0, buffer.Length)
                           _read) > 0 do
                        yield buffer.[0.._read - 1]
                })
            |> Seq.concat

        if buffer.Any () then
            do let readerWaveFormat =
                use reader = new AudioFileReader(Array.head slices)
                reader.WaveFormat

               use writer = new WaveFileWriter(combinedPath, readerWaveFormat)

               buffer
               |> Seq.iter (fun buffer -> writer.Write (buffer, 0, buffer.Length))

            wavTo16 combinedPath

    let ensureSameWavLengthAsync source destination =
        async {

            use sourceReader = new AudioFileReader(source)

            let destinationLength =
                use destinationReader = new AudioFileReader(destination)
                destinationReader.Length

            let diff = destinationLength - sourceReader.Length

            if diff < 0L then
                let destinationBad = destination + ".old"
                let destinationFiller = destination + ".filler"

                File.Move (destination, destinationBad)

                do use destinationFillerWriter = new WaveFileWriter(destinationFiller, sourceReader.WaveFormat)

                sourceReader.Position <- destinationLength

                let buffer = Array.zeroCreate<byte> (int -diff)

                sourceReader.Read (buffer, 0, buffer.Length)
                |> ignore

                destinationFillerWriter.Write (buffer, 0, buffer.Length)

                recombineAudio16
                    [|
                        destinationBad
                        destinationFiller
                    |]
                    destination

                File.Delete destinationFiller
                File.Delete destinationBad
            elif diff > 0L then
                failwith "Destination WAV larger than source"

        }

    let private ingestFileAsync (path: string) =
        async {
            Log.Debug ("\n\nIngesting file: {FileName}", Path.GetFileName path)

            let id =
                let id = Path.GetFileNameWithoutExtension path

                if Regexxer.hasMatch (id, "\[id=(\d+)\]") then
                    id
                else
                    id
                    + (sprintf " [id=%s]" (DateTime.Now.ToString "yyyyMMddHHmmssfff"))

            let tempFolder = FileSystem.ensureTempSessionDirectory ()

            let trackPath = Path.Combine ((SharedConfig.pathsLazyIo ()).dbTracks, id)
            Directory.CreateDirectory trackPath |> ignore

            let (tempMp3Path, tempWavPath) =
                ("mp3", "wav")
                |> Tuple2.map (fun ext -> Path.Combine (tempFolder, sprintf "all.%s" ext))

            try
                try

                    let createMp3 () =
                        use reader = new AudioFileReader(path)
                        MediaFoundationEncoder.EncodeToMp3 (reader, tempMp3Path, 160000)

                    let createWav () =
                        use reader = new AudioFileReader(path)
                        WaveFileWriter.CreateWaveFile16 (tempWavPath, reader)

                    [|
                        createMp3
                        createWav
                    |]
                    |> Array.Parallel.iter (fun x -> x ())

                    let splitWav path =
                        use reader = new AudioFileReader(path)

                        let sliceSeconds = 3. * 60.
                        let sliceCount = int (reader.TotalTime.TotalSeconds / sliceSeconds)

                        [|
                            0 .. sliceCount
                        |]
                        |> Array.Parallel.map (fun i ->
                            let startPos, endPos =
                                let getOffset i =
                                    int64
                                        (i
                                         * int sliceSeconds
                                         * (reader.WaveFormat.SampleRate
                                            * reader.WaveFormat.BlockAlign))

                                getOffset i, min reader.Length (getOffset (i + 1))

                            let newPath =
                                Path.Combine (Path.GetDirectoryName path, sprintf "%d.%s" i (Path.GetFileName path))

                            do use reader = new AudioFileReader(path)
                            use writer = new WaveFileWriter(newPath, reader.WaveFormat)

                            reader.Position <- startPos

                            let buffer = Array.zeroCreate<byte> 4096

                            while reader.Position < endPos do
                                let bytesRequired = endPos - reader.Position

                                if bytesRequired > 0L then
                                    let bytesToRead = min (int bytesRequired) buffer.Length
                                    let bytesRead = reader.Read (buffer, 0, bytesToRead)

                                    if bytesRead > 0 then
                                        writer.Write (buffer, 0, bytesRead)

                            wavTo16 newPath

                            newPath)

                    do! Bindings.layers
                        |> Seq.map (fun (layer, warm) ->
                            async {
                                let tempOutDir = Path.Combine (tempFolder, layer)
                                Directory.CreateDirectory tempOutDir |> ignore

                                try
                                    let destPeaksLevelsPath =
                                        Path.Combine
                                            (trackPath, sprintf "%s.%s.peaks.%s.dat" id layer Bindings.sources.Levels)

                                    let destPeaksPitchPath =
                                        Path.Combine
                                            (trackPath, sprintf "%s.%s.peaks.%s.dat" id layer Bindings.sources.Pitch)

                                    let destPitchPath =
                                        Path.Combine (trackPath, sprintf "%s.%s.%s.csv" id layer Bindings.sources.Pitch)

                                    let destPeaksLevelsPathExists = File.Exists destPeaksLevelsPath
                                    let destPitchPathExists = File.Exists destPitchPath

                                    let! sourcePath =
                                        async {
                                            let! slices =
                                                async {
                                                    let slices = splitWav tempWavPath

                                                    if warm = "" then
                                                        return slices
                                                    else
                                                        let openUnmixSeparationAsync i path =
                                                            async {
                                                                let modelId =
                                                                    sprintf "insanebrothers_%s_warm%s_v1" layer warm

                                                                let modelPath =
                                                                    Path.Combine
                                                                        ((SharedConfig.pathsLazyIo ()).extOpenUnmix,
                                                                         "states",
                                                                         modelId)

                                                                let inferenceScript =
                                                                    Path.Combine
                                                                        ((SharedConfig.pathsLazyIo ()).extOpenUnmix,
                                                                         "test.py")

                                                                let! unmixedPath =
                                                                    async {
                                                                        let cmd =
                                                                            sprintf
                                                                                """python "%s" --model "%s" --outdir "%s" "%s" --targets %s"""
                                                                                inferenceScript
                                                                                modelPath
                                                                                tempOutDir
                                                                                path
                                                                                layer

                                                                        let! errorCode, _ =
                                                                            Runtime.executeCondaAsync
                                                                                "mechahaze"
                                                                                [
                                                                                    cmd
                                                                                ]

                                                                        if errorCode <> 0 then
                                                                            failwith "Error executing open unmix"

                                                                        return
                                                                            Path.Combine
                                                                                (tempOutDir, sprintf "%d.wav" i)
                                                                    }

                                                                do! ensureSameWavLengthAsync path unmixedPath

                                                                return unmixedPath
                                                            }

                                                        return!
                                                            slices
                                                            |> Seq.mapi openUnmixSeparationAsync
                                                            |> Async.sequentialForced
                                                }

                                            if not destPitchPathExists then
                                                let extractPitchAsync path =
                                                    async {
                                                        let cmd =
                                                            sprintf
                                                                """crepe "%s" --step-size 5 --model-capacity full --output "%s" """
                                                                path
                                                                tempOutDir

                                                        let! errorCode, _ =
                                                            Runtime.executeCondaAsync
                                                                "mechahaze"
                                                                [
                                                                    cmd
                                                                ]

                                                        if errorCode <> 0 then
                                                            failwith "Error executing crepe"

                                                        return
                                                            Path.Combine
                                                                (tempOutDir,
                                                                 (Path.GetFileName path).Replace(".wav", ".f0.csv"))
                                                    }

                                                let! pitchSlices =
                                                    slices
                                                    |> Seq.map extractPitchAsync
                                                    |> Async.sequentialForced

                                                do! Waveform.recombineCsvAsync pitchSlices destPitchPath

                                            let combinedPath = Path.Combine (tempFolder, sprintf "%s.wav" layer)

                                            recombineAudio16 slices combinedPath

                                            return combinedPath
                                        }

                                    if not destPeaksLevelsPathExists then
                                        do! persistWaveformAsync sourcePath destPeaksLevelsPath

                                    if not destPitchPathExists then
                                        Waveform.pitchCsvToWaveform destPitchPath destPeaksLevelsPath destPeaksPitchPath
                                finally
                                    Directory.Delete (tempOutDir, true)
                            })
                        |> Async.sequentialForced
                        |> Async.Ignore

                    do! persistFingerprintAsync tempWavPath id

                    let destFileName = sprintf "%s.mp3" id

                    let destPath = Path.Combine (trackPath, destFileName)

                    if File.Exists destPath then
                        Log.Warning ("Track mp3 already exists")
                    else
                        Log.Debug ("Moving. Destination: {Destination}", destPath)
                        File.Copy (tempMp3Path, destPath, true)
                        File.Delete path
                with ex -> Log.Error (ex, "Error ingesting file")
            finally
                Directory.Delete (tempFolder, true)
        }


    let checkTrackHealthAsync path =
        async {
            let files = Directory.GetFiles (path, "*.*")

            if files.Length <> 28 then
                Log.Warning ("Track health error. Files found: {Length}. Path: {Path}", files.Length, path)
        }

    let listenAsync =
        async {
            Log.Debug ("TrackIngest listening start")

            while true do
                do! Directory.GetFiles ((SharedConfig.pathsLazyIo ()).ingestTracks, "*.*")
                    |> Array.map ingestFileAsync
                    |> Async.sequentialForced
                    |> Async.Ignore

                do! Async.Sleep 5000
        }

    let listenTracksHealthAsync =
        async {
            Log.Debug ("TrackIngest tracks health listening start")

            while true do
                do! Directory.GetDirectories (SharedConfig.pathsLazyIo ()).dbTracks
                    |> Array.map checkTrackHealthAsync
                    |> Async.Parallel
                    |> Async.Ignore

                do! Async.Sleep (60 * 1000)
        }
