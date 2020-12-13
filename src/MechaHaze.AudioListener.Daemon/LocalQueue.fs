namespace MechaHaze.AudioListener.Daemon

open MechaHaze.Shared

module LocalQueue =
    type Sample = { Buffer: byte []; Timestamp: int64 }

    type Event =
        | Error of exn
        | RecordingRequest
        | Recording of Sample
        | Match of SharedState.Track
        | ClientSetState of SharedState.SharedState
