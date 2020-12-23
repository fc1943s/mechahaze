namespace MechaHaze.UI.Frontend

open MechaHaze.Shared
open MechaHaze.UI
open MechaHaze.UI.Frontend
open MechaHaze.UI.Frontend.ElmishBridge


module MainView =
    let lazyView (props: Client.MainViewProps<SharedState.SharedServerMessage, UIState.State>) =

        let dispatch =
            InternalUI.SharedServerMessage
            >> Client.InternalServerMessage
            >> props.ServerToClientDispatch

        HomePageComponent.``default``
            {
                Dispatch = dispatch
                UIState = props.UIState
                PrivateState = props.PrivateState
            }
