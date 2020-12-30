namespace MechaHaze.UI.Backend

open System.Net
open System.Net.Security
open Giraffe.SerilogExtensions
open MechaHaze.CoreCLR.Core
open MechaHaze.Model
open MechaHaze.UI.Backend.ElmishBridge
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Serilog
open Thoth.Json.Giraffe
open MechaHaze.Shared
open Saturn
open Elmish.Bridge
open MechaHaze.UI
open System

module ServerBridge =
    open Elmish

    let init (clientDispatch: Dispatch<SharedState.Response>) (state: UIState.State) =
        clientDispatch (SharedState.Response.StateUpdated state.SharedState)
        state, Cmd.none

    let update (clientDispatch: Dispatch<SharedState.Response>) (msg: SharedState.Action) (state: UIState.State) =

        let withClientDispatch cmd (rsp: SharedState.Response) =
            clientDispatch rsp
            state, cmd

        match msg with
        | SharedState.SetOffset offset ->
            SharedState.Response.StateUpdated
                { state.SharedState with
                    Track = { state.SharedState.Track with Offset = offset }
                }
            |> withClientDispatch Cmd.none

        | SharedState.SetAutoLock autoLock ->
            SharedState.Response.StateUpdated { state.SharedState with AutoLock = autoLock }
            |> withClientDispatch Cmd.none

        | SharedState.SetRecordingMode recordingMode ->
            SharedState.Response.StateUpdated
                { state.SharedState with
                    RecordingMode = recordingMode
                }
            |> withClientDispatch Cmd.none

        | SharedState.SetLocked locked ->
            SharedState.Response.StateUpdated
                { state.SharedState with
                    Track =
                        { state.SharedState.Track with
                            Locked = locked
                            Offset =
                                if not locked then
                                    0.
                                else
                                    state.SharedState.Track.Offset
                        }
                }
            |> withClientDispatch Cmd.none


        | SharedState.ToggleBinding (Bindings.BindingToggle (presetName, binding)) ->

            let preset =
                state.SharedState.BindingsPresetMap
                |> Bindings.ofPresetMap
                |> Map.tryFind presetName
                |> Option.defaultValue (Bindings.Preset [])
                |> Bindings.applyBinding binding

            let presets =
                state.SharedState.BindingsPresetMap
                |> Bindings.ofPresetMap
                |> Map.add presetName preset
                |> Bindings.PresetMap

            SharedState.Response.StateUpdated
                { state.SharedState with
                    BindingsPresetMap = presets
                }
            |> withClientDispatch Cmd.none

        | SharedState.TogglePreset presetName ->
            let presets =
                let presetMap =
                    state.SharedState.BindingsPresetMap
                    |> Bindings.ofPresetMap

                presetMap
                |> Map.containsKey presetName
                |> function
                | true -> presetMap |> Map.remove presetName
                | false ->
                    presetMap
                    |> Map.add presetName (Bindings.Preset [])
                |> Bindings.PresetMap

            SharedState.Response.StateUpdated
                { state.SharedState with
                    BindingsPresetMap = presets
                }
            |> withClientDispatch Cmd.none

        | SharedState.SetActiveBindingsPreset presetName ->
            SharedState.Response.StateUpdated
                { state.SharedState with
                    ActiveBindingsPreset = presetName
                }
            |> withClientDispatch Cmd.none

module UIServer =
    let port =
        match Environment.GetEnvironmentVariable "PORT" with
        | null
        | "" -> string Bridge.Endpoints.apiPort
        | x -> x

    type UIServer () =

        //        let connections = Server.ServerToClientBridge<UIState.State, SharedState.Response, SharedState.Action> ()
//
//        member this.BroadcastTrack track =
//            None
//            |> Option.iter (fun _ ->
//                Log.Verbose
//                    ("Sending Track to clients. Connections: {@Connections}. State: {Track}",
//                     connections.GetConnectedUsers (),
//                     track))
//
//            connections.SharedBroadcastToClients (SharedState.TrackUpdated track)
//
//        member this.BroadcastState state =
//            None
//            |> Option.iter (fun _ ->
//                Log.Verbose
//                    ("Sending state to clients. Connections: {@Connections}. State: {Track}",
//                     connections.GetConnectedUsers (),
//                     state))
//
//            connections.InternalBroadcastToClients (InternalUI.InternalServerMessage.SetState state)


        member this.HangAsync (stateQueue: SafeQueue.SafeQueue<Server.StateScope<UIState.State>>) =
            async {

                //                let handleClientMessage message (state: UIState.State) __serverToClientDispatch =
//                    match message with
//                    | SharedState.SetOffset offset ->
//                        let state =
//                            { state with
//                                SharedState =
//                                    { state.SharedState with
//                                        Track = { state.SharedState.Track with Offset = offset }
//                                    }
//                            }
//
//                        state, None
//
//                    | SharedState.SetAutoLock autoLock ->
//                        { state with
//                            SharedState = { state.SharedState with AutoLock = autoLock }
//                        },
//                        None
//
//                    | SharedState.SetRecordingMode recordingMode ->
//                        { state with
//                            SharedState =
//                                { state.SharedState with
//                                    RecordingMode = recordingMode
//                                }
//                        },
//                        None
//
//                    | SharedState.SetLocked locked ->
//                        let state =
//                            { state with
//                                SharedState =
//                                    { state.SharedState with
//                                        Track =
//                                            { state.SharedState.Track with
//                                                Locked = locked
//                                                Offset =
//                                                    if not locked then
//                                                        0.
//                                                    else
//                                                        state.SharedState.Track.Offset
//                                            }
//                                    }
//                            }
//
//                        state, None
//
//                    | SharedState.ToggleBinding (Bindings.BindingToggle (presetName, binding)) ->
//                        let state =
//                            let preset =
//                                state.SharedState.BindingsPresetMap
//                                |> Bindings.ofPresetMap
//                                |> Map.tryFind presetName
//                                |> Option.defaultValue (Bindings.Preset [])
//                                |> Bindings.applyBinding binding
//
//                            let presets =
//                                state.SharedState.BindingsPresetMap
//                                |> Bindings.ofPresetMap
//                                |> Map.add presetName preset
//                                |> Bindings.PresetMap
//
//                            { state with
//                                SharedState =
//                                    { state.SharedState with
//                                        BindingsPresetMap = presets
//                                    }
//                            }
//
//                        state, None
//
//                    | SharedState.TogglePreset presetName ->
//                        let state =
//                            let presetMap =
//                                state.SharedState.BindingsPresetMap
//                                |> Bindings.ofPresetMap
//
//                            let presets =
//                                presetMap
//                                |> Map.containsKey presetName
//                                |> function
//                                | true -> presetMap |> Map.remove presetName
//                                | false ->
//                                    presetMap
//                                    |> Map.add presetName (Bindings.Preset [])
//                                |> Bindings.PresetMap
//
//                            { state with
//                                SharedState =
//                                    { state.SharedState with
//                                        BindingsPresetMap = presets
//                                    }
//                            }
//
//                        state, None
//
//                    | SharedState.SetActiveBindingsPreset presetName ->
//                        { state with
//                            SharedState =
//                                { state.SharedState with
//                                    ActiveBindingsPreset = presetName
//                                }
//                        },
//                        None

                //                let router' = Server.createRouter stateQueue connections handleClientMessage

                let bridge =
                    Bridge.mkServer Bridge.Endpoints.socketPath ServerBridge.init ServerBridge.update
                    |> Bridge.withConsoleTrace
                    |> Bridge.runWith Giraffe.server UIState.State.Default
                    |> SerilogAdapter.Enable

                let appRouter = router { get Bridge.Endpoints.socketPath bridge }

                ServicePointManager.ServerCertificateValidationCallback <-
                    RemoteCertificateValidationCallback (fun sender cert chain sslPolicyErrors ->
                        Log.Debug ("Validating cert: {0}; {1}", cert.GetCertHashString (), cert.GetRawCertDataString ())
                        true)

                let app =
                    application {
                        app_config Giraffe.useWebSockets

                        service_config (fun (services: IServiceCollection) ->
                            services.AddSingleton<Giraffe.Serialization.Json.IJsonSerializer> (ThothSerializer ()))

                        url $"{Bridge.Endpoints.protocol}://0.0.0.0:{Bridge.Endpoints.apiPort}/"
                        use_router appRouter
                        use_gzip
                        disable_diagnostics
                        use_developer_exceptions
                        force_ssl
                    }

                run app
            }
