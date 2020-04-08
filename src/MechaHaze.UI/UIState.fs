namespace MechaHaze.UI

open MechaHaze.Shared

module UIState =
    type State =
        { TimeSyncMap: SharedState.TimeSyncMap
          SharedState: SharedState.SharedState }
        static member inline Default =
            { TimeSyncMap = Map.empty |> SharedState.TimeSyncMap
              SharedState = SharedState.SharedState.Default }
        
