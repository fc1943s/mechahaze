namespace MechaHaze.CoreCLR.Core

open System.Reflection
open MechaHaze.Shared.Core


module Testing =
    let isTestingMemoizedLazy =
        fun () -> Assembly.GetEntryAssembly().GetName().Name = "testhost"
        |> Core.memoizeLazy
