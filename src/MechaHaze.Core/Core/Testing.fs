namespace MechaHaze.Core

open System.Reflection

module Testing =
    let isTestingMemoizedLazy =
        fun () -> Assembly.GetEntryAssembly().GetName().Name = "testhost"
        |> Core.memoizeLazy
