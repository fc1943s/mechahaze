namespace MechaHaze.Shared

open MechaHaze.Shared.Bindings

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


    type ProcessId = ProcessId of processId: string

    type TimeSyncMap = TimeSyncMap of Map<ProcessId, TimeSync>
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

    type TrackId = TrackId of string

    type Track =
        {
            Id: TrackId
            Position: float
            DurationSeconds: float
            DebugInfo: MatchDebugInfo
            Locked: bool
            Offset: float
            Timestamp: int64
        }
        static member inline Default =
            {
                Id = TrackId ""
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
            ActiveBindingsPreset: PresetId option
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
                //                BindingsPresetMap = PresetMap Map.empty
                BindingsPresetMap =
                    PresetMap [
                        PresetId "Simple",
                        {
                            Bindings =
                                [
                                    Binding (BindingSourceId "levels|vocals", BindingDestId "magic|vocals")
                                    Binding (BindingSourceId "levels|vocals2", BindingDestId "magic|vocals2")
                                ]
                        }
                        PresetId "Advanced",
                        {
                            Bindings =
                                [
                                    Binding (BindingSourceId "levels|vocals", BindingDestId "magic|vocals")
                                    Binding (BindingSourceId "levels|vocals", BindingDestId "magic|vocals2")
                                    Binding (BindingSourceId "", BindingDestId "magic|vocals3")
                                ]
                        }
                    ]
                ActiveBindingsPreset = None
            }

    let clean state =
        { SharedState.Default with
            Debug = state.Debug
            AutoLock = state.AutoLock
            RecordingMode = state.RecordingMode
            ActiveBindingsPreset = state.ActiveBindingsPreset
            BindingsPresetMap = state.BindingsPresetMap
        }


    type Action =
        | SetOffset of float
        | SetLocked of bool
        | SetAutoLock of bool
        | SetRecordingMode of bool
        | ToggleBinding of Bindings.BindingToggle
        | SetActiveBindingsPreset of PresetId option
        | TogglePreset of PresetId


    type Response =
        | TrackUpdated of Track
        | StateUpdated of SharedState
        | DebugUpdated of bool
        | OffsetUpdated of float
        | LockedUpdated of bool
        | AutoLockUpdated of bool
        | RecordingModeUpdated of bool
        | BindingToggled of Bindings.Binding
        | PresetToggled of PresetId
        | ActiveBindingsPresetUpdated of PresetId option


    type SharedQueue =
        | TrackUpdate of Track
        | StateUpdate of SharedState

        | ClientStateUpdate of SharedState
