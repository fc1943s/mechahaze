namespace MechaHaze.Daemon.AudioListener

open MechaHaze.Shared
open NAudio.Wave


module Audio =
    module NAudio =
        let waveFormat =
            use capture = new WasapiLoopbackCapture()
            capture.WaveFormat

    let averageSampleByteLength =
        int64 NAudio.waveFormat.AverageBytesPerSecond
        * int64 SharedState.sampleLengthSeconds
