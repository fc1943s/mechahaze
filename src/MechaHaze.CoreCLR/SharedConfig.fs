namespace MechaHaze.CoreCLR

open MechaHaze.CoreCLR.Core
open MechaHaze.Shared
open MechaHaze.Shared.Core
open Serilog
open System.IO
open System.Reflection
open Tomlyn
open Tomlyn.Model
open FSharp.Control.Tasks


module SharedConfig =

    let pathsMemoizedLazy =
        let envVars =
            {|
                MechaHazeHome = "MECHAHAZE_HOME"
                RabbitMQServer = "RABBITMQ_SERVER"
                OpenUnmixHome = "OPENUNMIX_HOME"
            |}

        fun () ->
            let mechaHazeHome =
                Environment.getRequiredEnvVar envVars.MechaHazeHome
                |> Result.unwrap

            let rabbitMQServer =
                Environment.getRequiredEnvVar envVars.RabbitMQServer
                |> Result.unwrap

            let openUnmixHome =
                Environment.getRequiredEnvVar envVars.OpenUnmixHome
                |> Result.unwrap

            let paths =
                {|
                    openUnmixHome = openUnmixHome
                    extAudiowaveformExe =
                        mechaHazeHome
                        </> "ext-audiowaveform-mingw64"
                        </> "audiowaveform.exe"
                    rabbitMQ =
                        {|
                            rabbitMQCtl = rabbitMQServer </> "sbin/rabbitmqctl.bat"
                        |}
                    mechaHaze =
                        {|
                            configToml = mechaHazeHome </> "config.toml"
                            dbFingerprints = mechaHazeHome </> "db-fingerprints"
                            dbTracks = mechaHazeHome </> "db-tracks"
                            dbState = mechaHazeHome </> "db-state"
                            ingestTracks = mechaHazeHome </> "ingest-tracks"
                            tempSamples = mechaHazeHome </> "temp-samples"
                        |}
                |}

            Directory.CreateDirectory paths.mechaHaze.tempSamples
            |> ignore

            paths

        |> Core.memoizeLazy

    type TomlConfig =
        {
            RabbitMqAddress: string
            RabbitMqUsername: string
            RabbitMqPassword: string
        }

    let loadToml () =
        task {
            Log.Debug ("Loading toml")

            try
                let! text = File.ReadAllTextAsync (pathsMemoizedLazy ()).mechaHaze.configToml
                let toml = Toml.Parse text
                let table = toml.ToModel ()
                let assemblyTable = table.[Assembly.GetEntryAssembly().GetName().Name] :?> TomlTable

                return
                    Ok
                        {
                            RabbitMqAddress = string assemblyTable.["rabbitmq_address"]
                            RabbitMqUsername = string assemblyTable.["rabbitmq_username"]
                            RabbitMqPassword = string assemblyTable.["rabbitmq_password"]
                        }
            with ex -> return Error ex
        }
