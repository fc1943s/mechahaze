namespace MechaHaze.Shared

module SharedState =
    let matchStabilityCount = 3
    let lockingNoiseCount = 9
    let sampleLengthSeconds = 5.
    let sampleIntervalSeconds = 2.5
    let sampleIntervalMs = sampleIntervalSeconds * 1000.



    type TimeSyncOffset = TimeSyncOffset of time: float * offset: float
    let ofTimeSyncOffset (TimeSyncOffset (a, b)) = a, b

    type TimeSync =
        {
            Offsets: TimeSyncOffset []
            StableOffsets: TimeSyncOffset []
        }
        static member inline Default = { Offsets = [||]; StableOffsets = [||] }


    type TimeSyncMap = TimeSyncMap of Map<string, TimeSync>
    let ofTimeSyncMap (TimeSyncMap x) = x






    type MatchDebugInfo =
        {
            Name: string
            TrackStartsAt: float
            TotalTracksAnalyzed: int
            TotalFingerprintsAnalyzed: int
            Score: float
            Confidence: float
            ResultEntriesLength: int
            AvgScoreAcrossBestPath: float
        }
        static member inline Default =
            {
                Name = ""
                TrackStartsAt = 0.
                TotalTracksAnalyzed = 0
                TotalFingerprintsAnalyzed = 0
                Score = 0.
                Confidence = 0.
                ResultEntriesLength = 0
                AvgScoreAcrossBestPath = 0.
            }

    type Track =
        {
            Id: string
            Position: float
            DurationSeconds: float
            DebugInfo: MatchDebugInfo
            Locked: bool
            Offset: float
            Timestamp: int64
        }
        static member inline Default =
            {
                Id = ""
                Position = 0.
                DurationSeconds = 0.
                DebugInfo = MatchDebugInfo.Default
                Locked = false
                Offset = 0.
                Timestamp = 0L
            }


    type SharedState =
        {
            Debug: bool
            Track: Track
            AutoLock: bool
            RecordingMode: bool
            ActiveBindingsPreset: string
            BindingsPresetMap: Bindings.PresetMap
        }

        static member inline Default =
            {
                Track = Track.Default
                Debug =
#if DEBUG
                    true
#else
                    false
#endif
                AutoLock = true
                RecordingMode = false
                BindingsPresetMap = Map.empty |> Bindings.PresetMap
                ActiveBindingsPreset = ""
            }

    let clean state =
        { SharedState.Default with
            Debug = state.Debug
            AutoLock = state.AutoLock
            RecordingMode = state.RecordingMode
            ActiveBindingsPreset = state.ActiveBindingsPreset
            BindingsPresetMap = state.BindingsPresetMap
        }


    type SharedClientMessage =
        | SetOffset of float
        | SetLocked of bool
        | SetAutoLock of bool
        | SetRecordingMode of bool
        | ToggleBinding of Bindings.BindingToggle
        | SetActiveBindingsPreset of string
        | TogglePreset of string


    type SharedServerMessage =
        | SetTrack of Track

        (* Client To Client *)
        | ClientSetDebug of bool
        | ClientSetOffset of float
        | ClientSetLocked of bool
        | ClientSetAutoLock of bool
        | ClientSetRecordingMode of bool
        | ClientToggleBinding of Bindings.Binding
        | ClientTogglePreset of string
        | ClientSetActiveBindingsPreset of string


    type SharedQueue =
        | TrackUpdate of Track
        | StateUpdate of SharedState

        | ClientStateUpdate of SharedState
