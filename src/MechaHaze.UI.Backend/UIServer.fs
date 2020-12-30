namespace MechaHaze.UI.Backend

open MechaHaze.Model
open MechaHaze.UI.Backend.ElmishBridge
open Serilog
open MechaHaze.Shared
open Saturn
open Elmish.Bridge
open MechaHaze.UI
open System


module UIServer =
    let port =
        match Environment.GetEnvironmentVariable "PORT" with
        | null
        | "" -> string Bridge.Endpoints.apiPort
        | x -> x

    type UIServer () =

        let connections =
            Server.ServerToClientBridge<UIState.State, SharedState.SharedServerMessage, SharedState.SharedClientMessage>
                ()

        member this.BroadcastTrack track =
            None
            |> Option.iter (fun _ ->
                Log.Verbose
                    ("Sending Track to clients. Connections: {@Connections}. State: {Track}",
                     connections.GetConnectedUsers (),
                     track))

            connections.SharedBroadcastToClients (SharedState.SetTrack track)

        member this.BroadcastState state =
            None
            |> Option.iter (fun _ ->
                Log.Verbose
                    ("Sending state to clients. Connections: {@Connections}. State: {Track}",
                     connections.GetConnectedUsers (),
                     state))

            connections.InternalBroadcastToClients (InternalUI.InternalServerMessage.SetState state)


        member this.HangAsync stateQueue =
            async {

                let handleClientMessage message (state: UIState.State) __serverToClientDispatch =
                    match message with
                    | SharedState.SetOffset offset ->
                        let state =
                            { state with
                                SharedState =
                                    { state.SharedState with
                                        Track = { state.SharedState.Track with Offset = offset }
                                    }
                            }

                        state, None

                    | SharedState.SetAutoLock autoLock ->
                        { state with
                            SharedState = { state.SharedState with AutoLock = autoLock }
                        },
                        None

                    | SharedState.SetRecordingMode recordingMode ->
                        { state with
                            SharedState =
                                { state.SharedState with
                                    RecordingMode = recordingMode
                                }
                        },
                        None

                    | SharedState.SetLocked locked ->
                        let state =
                            { state with
                                SharedState =
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
                            }

                        state, None

                    | SharedState.ToggleBinding (Bindings.BindingToggle (presetName, binding)) ->
                        let state =
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

                            { state with
                                SharedState =
                                    { state.SharedState with
                                        BindingsPresetMap = presets
                                    }
                            }

                        state, None

                    | SharedState.TogglePreset presetName ->
                        let state =
                            let presetMap =
                                state.SharedState.BindingsPresetMap
                                |> Bindings.ofPresetMap

                            let presets =
                                presetMap
                                |> Map.containsKey presetName
                                |> function
                                | true -> presetMap |> Map.remove presetName
                                | false ->
                                    presetMap
                                    |> Map.add presetName (Bindings.Preset [])
                                |> Bindings.PresetMap

                            { state with
                                SharedState =
                                    { state.SharedState with
                                        BindingsPresetMap = presets
                                    }
                            }

                        state, None

                    | SharedState.SetActiveBindingsPreset presetName ->
                        { state with
                            SharedState =
                                { state.SharedState with
                                    ActiveBindingsPreset = presetName
                                }
                        },
                        None

                let router' = Server.createRouter stateQueue connections handleClientMessage

                let app =
                    application {
                        url Bridge.Endpoints.apiBaseUrl
                        app_config Giraffe.useWebSockets
                        use_router router'
                        use_gzip
                        disable_diagnostics
                        force_ssl
                    }

                run app
            }
