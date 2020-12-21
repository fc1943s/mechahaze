namespace MechaHaze.Core

open System


module Environment =
    let getRequiredEnvVar name =
        match Environment.GetEnvironmentVariable name with
        | null
        | "" -> Error $"Invalid EnvVar: {name}"
        | s -> s |> Ok

