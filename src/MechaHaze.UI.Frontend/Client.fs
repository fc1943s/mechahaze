namespace MechaHaze.UI.Frontend

open MechaHaze.Shared
open MechaHaze.UI
open MechaHaze.UI.Frontend
open Suigetsu.UI.Frontend.ElmishBridge

module Client =
    let inline handleClientMessage (message: SharedState.SharedServerMessage) (state: UIState.State) =
        match message with
        | SharedState.SetTrack track ->
            let state =
                { state with
                    SharedState = { state.SharedState with Track = track }
                }

            state, None

        | SharedState.ClientSetDebug debug ->
            let state =
                { state with
                    SharedState = { state.SharedState with Debug = debug }
                }

            state, None

        | SharedState.ClientSetOffset offset ->
            let state =
                { state with
                    SharedState =
                        { state.SharedState with
                            Track = { state.SharedState.Track with Offset = offset }
                        }
                }

            state, Some (SharedState.SetOffset offset)

        | SharedState.ClientSetAutoLock autoLock ->
            let state =
                { state with
                    SharedState = { state.SharedState with AutoLock = autoLock }
                }

            state, Some (SharedState.SetAutoLock autoLock)

        | SharedState.ClientSetRecordingMode recordingMode ->
            let state =
                { state with
                    SharedState =
                        { state.SharedState with
                            RecordingMode = recordingMode
                        }
                }

            state, Some (SharedState.SetRecordingMode recordingMode)

        | SharedState.ClientSetLocked locked ->
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

            state, Some (SharedState.SetLocked locked)

        | SharedState.ClientToggleBinding binding ->
            state,
            Some (SharedState.ToggleBinding (Bindings.BindingToggle (state.SharedState.ActiveBindingsPreset, binding)))

        | SharedState.ClientTogglePreset presetName -> state, Some (SharedState.TogglePreset presetName)

        | SharedState.ClientSetActiveBindingsPreset presetName ->
            let state =
                { state with
                    SharedState =
                        { state.SharedState with
                            ActiveBindingsPreset = presetName
                        }
                }

            state, Some (SharedState.SetActiveBindingsPreset presetName)

    let listen () =
        Client.listen<UIState.State, SharedState.SharedServerMessage, SharedState.SharedClientMessage>
            UIState.State.Default
            MainView.lazyView
            handleClientMessage
            true

    listen ()
