namespace MechaHaze.Shared.CoreCLR

open FSharp.Data
open System.IO
open Suigetsu.Core

module Waveform =
    
    [<Literal>]
    let PitchSchemaSample = "Time,Frequency,Confidence"

    type PitchSchema = CsvProvider<Sample = PitchSchemaSample,
                                   Schema = "Time (decimal), Frequency (decimal), Confidence (decimal)",
                                   HasHeaders = true>

    type WaveformHeaderV2 =
        { Flags: uint32
          SampleRate: int32
          SamplesPerPixel: int32
          Length: uint32
          Channels: int32 }

    type WaveformModel =
        { Version: int32
          Header: WaveformHeaderV2
          ChannelL: float[]
          ChannelR: float[] }

    let DEFAULT_WAVEFORM_VERSION = 2

    let normalize (channels: Map<int, float[]>) =
        let max =
            channels
            |> Map.values
            |> Seq.collect (fun x -> x)
            |> Seq.max

        channels
        |> Map.map (fun _ v ->
            let ratio = 100. / max
            v
            |> Array.map ((*) ratio)
            |> Array.map (flip (/) 100.)
        )

    let readPeaks fileName =
        use reader = new BinaryReader (File.Open (fileName, FileMode.Open, FileAccess.Read, FileShare.Read))

        let version = reader.ReadInt32 ()

        if version <> DEFAULT_WAVEFORM_VERSION then
            failwithf "Invalid waveform version: %d" version

        let header =
            (reader.ReadUInt32 (),
             reader.ReadInt32 (),
             reader.ReadInt32 (),
             reader.ReadUInt32 (),
             reader.ReadInt32 ())
            |> fun (flags, sampleRate, samplesPerPixel, length, channels) ->
                { Flags = flags
                  SampleRate = sampleRate
                  SamplesPerPixel = samplesPerPixel
                  Length = length
                  Channels = channels }

        let channels =
            seq { for _ in 0u .. header.Length - 1u do
                      for channel in 0 .. header.Channels - 1 do
                          yield channel, (reader.ReadByte (), reader.ReadByte ())
                                          |> fun (min, max) -> max - min
                                          |> float }
            |> Seq.groupBy fst
            |> Map.ofSeq
            |> Map.map (fun _ v ->
                v
                |> Seq.map snd
                |> Seq.toArray
            )
            |> normalize

        { Version = version
          Header = header
          ChannelL = channels.Item 0
          ChannelR = channels.Item 1 }

    let writePeaks data fileName =
        use writer = new BinaryWriter (File.Open (fileName, FileMode.Create))

        writer.Write data.Version
        writer.Write data.Header.Flags
        writer.Write data.Header.SampleRate
        writer.Write data.Header.SamplesPerPixel
        writer.Write data.Header.Length
        writer.Write data.Header.Channels

        let normalized =
            [ data.ChannelL; data.ChannelR ]
            |> List.indexed
            |> Map.ofList
            |> normalize


        normalized.Item 0
        |> Seq.zip (normalized.Item 1)
        |> Seq.collect (fun (r, l) -> [ -l; l; -r; r ])
        |> Seq.map ((*) 100.)
        |> Seq.map byte
        |> Seq.iter writer.Write



    let pitchCsvToWaveform (csvPath: string) peaksReferencePath destinationPath =
        let pitchRows =
            let pitchCsv = PitchSchema.Load csvPath

            let pitchRows =
                pitchCsv.Rows
                |> Seq.scan (fun x y -> if y.Confidence > 0.3m then y.Frequency else x) 0m
                |> Seq.map float
                |> Seq.toArray

            let normalized =
                seq { 0, pitchRows }
                |> Map.ofSeq
                |> normalize

            normalized.Item 0

        let reference = readPeaks peaksReferencePath

        let peaks =
            seq { 0 .. reference.ChannelL.Length - 1 }
            |> Seq.map (fun i ->
                let peaksPitchIndex = float i * (float pitchRows.Length / float reference.ChannelL.Length) |> int
                let pitch = pitchRows.[peaksPitchIndex] / 100.
                pitch
            )
            |> Seq.toArray

        writePeaks
            { reference with
                  ChannelL = peaks
                  ChannelR = peaks } destinationPath


    let recombineCsvAsync slices destPath = async {
        let! slices =
            slices
            |> Seq.map (fun (path: string) -> async {
                use! csv = PitchSchema.AsyncLoad path
                return csv.Rows |> Seq.toArray 
            })
            |> Async.Parallel

        let slices =
            slices
            |> Array.mapi (fun i rows ->
                if i = 0
                then rows
                else
                    let previousRowTime =
                        ((slices.[i - 1] |> Seq.last).Time * decimal i)
                        + (slices.[0].[1].Time * decimal i)
                        
                    rows
                    |> Array.map (fun row -> PitchSchema.Row (row.Time + previousRowTime, row.Frequency, row.Confidence))
            )
            |> Array.concat
            
        if slices.Length > 0 then
            File.WriteAllText (destPath, PitchSchemaSample.ToLower ())
            use! destCsv = PitchSchema.AsyncLoad destPath
            use modified = destCsv.Append slices
            modified.Save destPath
    }
