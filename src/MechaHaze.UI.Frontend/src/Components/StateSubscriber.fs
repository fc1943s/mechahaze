namespace MechaHaze.UI.Frontend.Components

open FSharpPlus
open Fable.React
open Feliz.Recoil
open Feliz
open MechaHaze.Shared
open MechaHaze.UI
open MechaHaze.UI.Frontend
open MechaHaze.Shared.Bindings

module StateSubscriber =
    [<ReactComponent>]
    let stateSubscriber () =
        let uiState = Recoil.useValue Atoms.uiState

        let updateData =
            Recoil.useCallbackRef (fun setter (state: UIState.State) ->
                setter.set (Atoms.debug, state.SharedState.Debug)
                setter.set (Atoms.autoLock, state.SharedState.AutoLock)
                setter.set (Atoms.recordingMode, state.SharedState.RecordingMode)
                setter.set (Atoms.activeBindingsPreset, state.SharedState.ActiveBindingsPreset)

                setter.set (AtomFamilies.Track.locked state.SharedState.Track.Id, state.SharedState.Track.Locked)
                setter.set (AtomFamilies.Track.offset state.SharedState.Track.Id, state.SharedState.Track.Offset)
                setter.set (AtomFamilies.Track.position state.SharedState.Track.Id, state.SharedState.Track.Position)
                setter.set (AtomFamilies.Track.timestamp state.SharedState.Track.Id, state.SharedState.Track.Timestamp)
                setter.set (AtomFamilies.Track.debugInfo state.SharedState.Track.Id, state.SharedState.Track.DebugInfo)

                setter.set
                    (AtomFamilies.Track.durationSeconds state.SharedState.Track.Id,
                     state.SharedState.Track.DurationSeconds)

                state.SharedState.BindingsPresetMap
                |> ofPresetMap
                |> Map.iter (fun presetId preset -> setter.set (AtomFamilies.preset presetId, preset))

                state.TimeSyncMap
                |> SharedState.ofTimeSyncMap
                |> Map.iter (fun processId timeSync -> setter.set (AtomFamilies.timeSync processId, timeSync))

                let presetIdList =
                    state.SharedState.BindingsPresetMap
                    |> ofPresetMap
                    |> Map.keys
                    |> Seq.toList

                setter.set (Atoms.presetIdList, presetIdList)

                let processIdList =
                    state.TimeSyncMap
                    |> SharedState.ofTimeSyncMap
                    |> Map.keys
                    |> Seq.toList

                setter.set (Atoms.processIdList, processIdList)

                ())

        React.useEffect
            ((fun () -> updateData uiState),
             [|
                 box uiState
             |])

        nothing
