namespace MechaHaze.CoreCLR

open MechaHaze.CoreCLR.Core
open MechaHaze.Shared
open MechaHaze.Shared.Core
open Serilog
open System.IO
open System.Reflection
open Tomlyn
open Tomlyn.Model


module SharedConfig =

    let pathsLazyIo =
        let envVars = {| RootPath = "MECHAHAZE_HOME" |}

        fun () ->
            let rootPath =
                Environment.getRequiredEnvVar envVars.RootPath
                |> Result.okOrThrow

            let paths =
                {|
                    configToml = Path.Combine (rootPath, "config.toml")
                    dbFingerprints = Path.Combine (rootPath, "db-fingerprints")
                    dbTracks = Path.Combine (rootPath, "db-tracks")
                    dbState = Path.Combine (rootPath, "db-state")
                    extAudiowaveformExe = Path.Combine (rootPath, "ext-audiowaveform-mingw64", "audiowaveform.exe")
                    ingestTracks = Path.Combine (rootPath, "ingest-tracks")
                    tempSamples = Path.Combine (rootPath, "temp-samples")
                    extOpenUnmix = @"D:\kal\fs\git-repos\open-unmix-pytorch"
                |}

            Directory.CreateDirectory paths.tempSamples
            |> ignore

            paths

        |> Core.memoizeLazy

    type TomlConfig =
        {
            RabbitMqAddress: string
            RabbitMqUsername: string
            RabbitMqPassword: string
        }

    let loadTomlIo () =
        Log.Debug ("Loading toml")

        let toml = Toml.Parse (File.ReadAllText (pathsLazyIo ()).configToml)

        let table = toml.ToModel ()

        let assemblyTable = table.[Assembly.GetEntryAssembly().GetName().Name] :?> TomlTable

        {
            RabbitMqAddress = string assemblyTable.["rabbitmq_address"]
            RabbitMqUsername = string assemblyTable.["rabbitmq_username"]
            RabbitMqPassword = string assemblyTable.["rabbitmq_password"]
        }