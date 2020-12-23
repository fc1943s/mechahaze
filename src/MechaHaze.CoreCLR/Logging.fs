namespace MechaHaze.CoreCLR

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Serilog
open Serilog.Core
open System.Linq
open Serilog.Debugging
open Serilog.Events
open MechaHaze.Core


module Logging =

    // TODO: idiomatic f# rewrite
    type private DelegateLogEventSink() =
        static let mutable _sinks = ConcurrentBag<ILogEventSink>()

        interface ILogEventSink with
            member _.Emit logEvent =
                let emit () =
                    _sinks |> Seq.iter (fun x -> x.Emit logEvent)

                if Testing.isTestingMemoizedLazy ()
                   || logEvent.Level >= LogEventLevel.Warning then
                    emit ()
                else
                    async { emit () } |> Async.Start

        static member AddSink(sink: ILogEventSink) = _sinks.Add sink

    type private FilterList = IList<(string * LogEventLevel)>

    let private PROP_SOURCE_CONTEXT = "SourceContext"

    type private LogFilter(filters: FilterList, __excludingFilters: unit) =

        let filters =
            filters
                .GroupBy(fun (x, _) -> x)
                .Select(fun x -> x.Last())
                .OrderByDescending(fun (x, _) -> x.Length)
                .ToList()

        interface ILogEventFilter with
            member _.IsEnabled logEvent =
                let property =
                    logEvent.Properties.TryGetValue PROP_SOURCE_CONTEXT
                    |> snd

                match property with
                | :? ScalarValue as scalarProperty ->
                    let value = string scalarProperty.Value

                    // Log.Debug ("Log filter: {A}", value)

                    filters
                    |> Seq.tryFind (fun (ns, _) -> value.StartsWith ns)
                    |> function
                    | Some (_, level) -> logEvent.Level >= level
                    | None -> true
                | _ -> true

    type private ClassLogEnricher() =
        interface ILogEventEnricher with
            member _.Enrich(logEvent, propertyFactory) =
                if not (logEvent.Properties.ContainsKey PROP_SOURCE_CONTEXT) then
                    let initialStack = Runtime.getStackTrace ()

                    let stack =
                        initialStack
                            .SkipWhile(fun x ->
                            not (x.Contains "Serilog.Log.Write")
                            && not (x.Contains "Serilog.Core.Logger.Write"))
                            .ToList()

                    if stack.Count > 0 then
                        let className =
                            stack
                                .Select(fun x -> Regexxer.matchFirst (x, @"at ([\w\.`]+[^.])\..*?\("))
                                .FirstOrDefault(fun (x: string) -> x.Contains "Serilog" |> not)

                        logEvent.AddOrUpdateProperty
                            (propertyFactory.CreateProperty(PROP_SOURCE_CONTEXT, $"{className}*"))

    let consoleSink (config: LoggerConfiguration) = config.WriteTo.Console()

    let addLoggingSink (sinkConfig: LoggerConfiguration -> LoggerConfiguration) verbose =
        let loggerConfiguration =
            sinkConfig(LoggerConfiguration())
                .Destructure.FSharpTypes()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Stage", "Stage")
                .Enrich.WithProperty("Audience", "Audience")
                .Enrich.WithProperty("Hostname", "Hostname")
                .Enrich.With<ClassLogEnricher>()

        SelfLog.Enable Console.Out

        // TODO: external configuration
        let filters =
            List<_>
                ([ "EasyNetQ", LogEventLevel.Information
                   if not verbose then
                       "System", LogEventLevel.Debug
                       "PulsarClient", LogEventLevel.Information
                       "Microsoft.AspNetCore.Hosting", LogEventLevel.Warning ])

        let excludingFilters = ()

        DelegateLogEventSink.AddSink
            (loggerConfiguration
                .MinimumLevel
                .Verbose()
                .Filter.With(LogFilter(filters, excludingFilters))
                .CreateLogger())

        if Log.Logger = null
           || Log.Logger.GetType()
              <> typeof<DelegateLogEventSink> then
            Log.Logger <-
                (LoggerConfiguration())
                    .MinimumLevel.Is(if verbose then LogEventLevel.Verbose else LogEventLevel.Debug)
                    .WriteTo.Sink(DelegateLogEventSink())
                    .CreateLogger()
