#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Target
//"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO.Globbing.Operators



Target.initEnvironment ()

Target.create "Clean" (fun _ -> Trace.trace " --- Cleaning stuff (empty) --- ")

Target.create
    "Build"
    (fun _ ->
        Trace.trace " --- Building the app --- "

        let dotnetRelease (option: DotNet.CliInstallOptions) =
            { option with
                  InstallerOptions = (fun io -> { io with Branch = "release/5.0" })
                  Channel = None
                  Version = DotNet.Version "5.0.100" }

        let install = lazy (DotNet.install dotnetRelease)

        let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

        let inline dotnetSimple2 (arg: Fake.DotNet.DotNet.BuildOptions) =
            { arg with
                  NoRestore = true
                  Configuration = DotNet.BuildConfiguration.Debug }

        DotNet.build dotnetSimple2 "../MechaHaze.IO.fsproj"
        |> ignore

        DotNet.exec dotnetSimple "fsi" "watch.fsx"
        |> ignore

        ())

Target.create "Watch" (fun _ -> Trace.trace " --- Watching app --- ")

open Fake.Core.TargetOperators

"Clean" //
==> "Build"
==> "Watch"

Target.runOrDefault "Watch"
