namespace MechaHaze.CoreCLR.Core

open System
open System.IO
open System.Reflection
open MechaHaze.Shared.Core


module FileSystem =
    let ensureTempSessionDirectory () =
        let tempFolder =
            Path.GetTempPath ()
            </> Assembly.GetEntryAssembly().GetName().Name
            </> string (Guid.NewGuid ())

        Directory.CreateDirectory tempFolder |> ignore

        tempFolder
