#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Target
//"
#load "./.fake/build.fsx/intellisense.fsx"
#load "../MechaHaze.Interop.OperatingSystem/Interactive/utils.fsx"

open Fake.Core
open Fake.DotNet


Target.create "Clean" (fun _ -> Trace.trace " --- Cleaning the app --- ")

Target.create "Build" (fun _ ->
    Trace.trace " --- Building the app --- "

    let buildParams (args: DotNet.BuildOptions) =
        { args.WithCommon Utils.Interactive.getDotNetRelease with
              NoRestore = true
              Configuration = DotNet.BuildConfiguration.Debug }

    DotNet.build buildParams "./MechaHaze.Daemon.AudioListener.fsproj"
    ())

Target.create "Watch" (fun _ ->
    Trace.trace " --- Watching app --- "

    DotNet.exec Utils.Interactive.getDotNetRelease "fsi" "../MechaHaze.Interop.OperatingSystem/Interactive/watch.fsx"
    |> ignore)

open Fake.Core.TargetOperators

"Clean" //
==> "Build"
==> "Watch"

Target.runOrDefault "Watch"
