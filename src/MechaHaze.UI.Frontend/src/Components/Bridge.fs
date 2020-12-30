namespace MechaHaze.UI.Frontend.Components

open Elmish
open Elmish.Bridge
open Feliz
open Feliz.Recoil
open MechaHaze.Model
open MechaHaze.Shared
open MechaHaze.UI
open MechaHaze.UI.Frontend
open Feliz.Recoil.Bridge


module Client =
    let inline update (message: SharedState.Response) (state: UIState.State) =
        match message with
        | SharedState.StateUpdated sharedState -> { state with SharedState = sharedState }

        | SharedState.TrackUpdated track ->
            { state with
                SharedState = { state.SharedState with Track = track }
            }

        | SharedState.ClientSetDebug debug ->
            { state with
                SharedState = { state.SharedState with Debug = debug }
            }

        | SharedState.ClientSetOffset offset ->
            { state with
                SharedState =
                    { state.SharedState with
                        Track = { state.SharedState.Track with Offset = offset }
                    }
            }

        | SharedState.ClientSetAutoLock autoLock ->
            { state with
                SharedState = { state.SharedState with AutoLock = autoLock }
            }

        | SharedState.ClientSetRecordingMode recordingMode ->
            { state with
                SharedState =
                    { state.SharedState with
                        RecordingMode = recordingMode
                    }
            }

        | SharedState.ClientSetLocked locked ->
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


        | SharedState.ClientToggleBinding binding -> state
        //  , Some (SharedState.ToggleBinding (Bindings.BindingToggle (state.SharedState.ActiveBindingsPreset, binding)))

        | SharedState.ClientTogglePreset presetName -> state
        //, Some (SharedState.TogglePreset presetName)

        | SharedState.ClientSetActiveBindingsPreset presetName ->
            { state with
                SharedState =
                    { state.SharedState with
                        ActiveBindingsPreset = presetName
                    }
            }

module Bridge =
    let bridge =
        Recoil.bridge  //<UIState.State, SharedState.Response, SharedState.Response>
            {
                Model = Atoms.uiState
                Update = Client.update
                BridgeConfig =
                    Bridge.endpoint Bridge.Endpoints.socketPath
//                    |> Bridge.withUrlMode UrlMode.Raw
            }
