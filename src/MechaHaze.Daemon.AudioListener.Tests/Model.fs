namespace MechaHaze.Daemon.AudioListener

open System
open Spreads.Serialization.Utf8Json.Formatters

// TDD after?
module Model =

    type Track = { Id: string; Timestamp: int64 }

    type Timestamp = Timestamp of int64


module AudioListener =
    type Buffer = Buffer of byte []

    type Sample = { Timestamp: Model.Timestamp; Buffer: Buffer }

    type X =
        | Sample of Model.Timestamp * Buffer
        | Track of Model.Track


    type RecordSample = Async<Sample>

    type QuerySample = Sample -> Async<Model.Track option>

    type StabilizeTrack = Model.Track * Model.Track -> Model.Track option

    type LockTrack = Model.Track -> bool * float

    type StableTrack = StableTrack of Model.Track

    type TrackEntry =
        | Raw of Sample
        | Unstable of Model.Track
        | Stable of StableTrack
        | Locked

    type TrackAnalysis =
        | Stabilize of (Model.Track * Model.Track -> Model.Track option)
        | Lock of ((Model.Track -> bool * float))

    type TrackService =
        | Query of Sample
        | Stabilize of old: Model.Track * new': Model.Track
        | Lock of Model.Track


module Bindings =
    type Pan =
        | L
        | R

    type Source =
        | Volume
        | Pitch
        | Pan of Pan

    type Destination =
        | Magic
        | Resolume

    type NodeType =
        | Source of Source
        | Destination of Destination

    type Node = { Type: NodeType; Id: string }

    type ParseNode = string -> Node

    type Binding = { Source: Node; Destination: Node }

    type Preset = Preset of Binding list

    type ApplyBinding = Binding -> Preset -> Preset

    type PresetMap = PresetMap of Map<string, Preset>

    type BindingToggle = { PresetName: string; Binding: Binding }


module FeatureDispatcher =
    type Message = { Node: Bindings.Node; Value: float }

    type X =
        | Message of Message
        | Bundle of Message seq

    type Send = X -> unit


module TimeSync =
    type TimeSyncOffset = { Time: float; Offset: float }

    type TimeSync =
        {
            Offsets: TimeSyncOffset []
            StableOffsets: TimeSyncOffset []
        }

    type TimeSyncMap = TimeSyncMap of Map<string, TimeSync>


module State =
    type Config =
        {
            AutoLock: bool
            RecordingMode: bool
            ActiveBindingsPreset: string
            BindingsPresetMap: Bindings.PresetMap
        }

    type UI =
        {
            TimeSyncMap: TimeSync.TimeSyncMap
            Config: Config
        }


module TrackIngest =
    let ingestFile = 0


module OpenUnmix =

    type X =
        | Training
        | Inference


    let a (|TST|GG|) x =
        match x with
        | TST AE -> AE
        | GG W -> W

    let (|FF|_|) q =
        match q with
        | 0 -> Some 0
        | _ -> None

    let (|GG|_|) q =
        match q with
        | 0 -> Some 0
        | _ -> None

    let (|FF|GG|) q =
        match q with
        | 0 -> FF
        | _ -> GG

    let (|FF|GG|N|) q =
        match q with
        | 0 -> FF
        | 1 -> N
        | _ -> GG

    let aa1 = a (|FF|GG|) 0
    let aa2 = a (|FF|GG|) 0

    type OptionalData (?a: int, ?b: string) =
        class
        end

    OptionalData (b = "") |> ignore
