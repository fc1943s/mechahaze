namespace MechaHaze.CoreCLR.Core

open System
open System.Reflection
open Argu
open MechaHaze.CoreCLR.Core
open Serilog

module Startup =
    let withLogging verbose fn =
        Logging.addLoggingSink Logging.consoleSink verbose

        try
            try
                fn ()

                Log.Information ("Program end")
                0
            with ex ->
                Log.Error (ex, "Program error")
                1
        finally
            Log.CloseAndFlush ()

    let parseArgsIo<'T when 'T :> IArgParserTemplate> args =
        let errorHandler =
            ProcessExiter
                (colorizer =
                    function
                    | ErrorCode.HelpText -> None
                    | _ -> Some ConsoleColor.Red)

        let parser =
            ArgumentParser.Create<'T>
                (programName =
                    Assembly.GetEntryAssembly().GetName().Name
                    + ".exe",
                 errorHandler = errorHandler)

        parser.ParseCommandLine args
