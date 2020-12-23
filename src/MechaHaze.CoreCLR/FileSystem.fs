namespace MechaHaze.CoreCLR

open System
open System.IO
open System.Reflection


module FileSystem =
    let ensureTempSessionDirectory () =
        let tempFolder =
            Path.Combine (Path.GetTempPath (), Assembly.GetEntryAssembly().GetName().Name, string (Guid.NewGuid ()))

        Directory.CreateDirectory tempFolder |> ignore

        tempFolder
