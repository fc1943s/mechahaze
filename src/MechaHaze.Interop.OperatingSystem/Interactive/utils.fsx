#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.Core.Target
//"
//#r "nuget: Fake.IO.FileSystem"
//#r "nuget: Fake.DotNet.Cli"
//#r "nuget: Fake.Core.Target"

open Fake.Core
open Fake.DotNet


module Interactive =
    let getDotNetRelease: DotNet.Options -> DotNet.Options =
        let installOptions (option: DotNet.CliInstallOptions) =
            { option with
                  InstallerOptions = (fun io -> { io with Branch = "release/5.0" })
                  Channel = None
                  Version = DotNet.Version "5.0.100" }

        let install = lazy (DotNet.install installOptions)

        let inline dotNetBase arg = DotNet.Options.lift install.Value arg

        dotNetBase
