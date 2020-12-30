namespace MechaHaze.UI.Frontend.ElmishBridge

open Elmish.Bridge
open Elmish
open Elmish.React
open Fable.Core
open Fable.React
open MechaHaze.Model
open MechaHaze.UI

#if DEBUG
open Elmish.HMR
#endif

module Client =

    type PrivateServerMessage<'SharedServerMessage, 'UIState> =
        | InternalServerMessage of InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>
        | ConnectionLost

    type Connection =
        | Disconnected
        | Connected

    type PrivateState<'UIState> =
        {
            Connection: Connection
            User: InternalUI.User
            Users: InternalUI.User list
            UIState: 'UIState
        }

    type MainViewProps<'SharedServerMessage, 'UIState> =
        {
            UIState: 'UIState
            PrivateState: PrivateState<'UIState>
            ServerToClientDispatch: PrivateServerMessage<'SharedServerMessage, 'UIState> -> unit
        }

    let inline listen<'UIState, 'SharedServerMessage, 'SharedClientMessage> (initialState: 'UIState)
                                                                            (lazyView: MainViewProps<'SharedServerMessage, 'UIState> -> ReactElement)
                                                                            (handler: 'SharedServerMessage -> 'UIState -> ('UIState * 'SharedClientMessage option))
                                                                            (bridge: bool)
                                                                            =

        let init () =
            let state =
                {
                    Connection = Disconnected
                    User =
                        {
                            InternalUI.Id = string (System.Random().Next())
                        }
                    Users = []
                    UIState = initialState
                }

            printfn "Initial state: %s" (JS.JSON.stringify state)
            state, Cmd.none

        let inline update (msg: PrivateServerMessage<'SharedServerMessage, 'UIState>) (state: PrivateState<'UIState>) =
            //  printfn "\nNew message (update): %s. \nState: %s" (JS.JSON.stringify msg) (JS.JSON.stringify state)

            let newState, cmd =
                match msg with
                | ConnectionLost ->
                    (match state.Connection with
                     | Disconnected -> state
                     | _ ->
                         { state with
                             Connection = Disconnected
                             Users = []
                         }),
                    None

                | InternalServerMessage msg ->
                    match msg with
                    | InternalUI.WelcomeUser (users, sharedState) ->
                        { state with
                            Users = users
                            UIState = sharedState
                        },
                        Some (InternalUI.InternalClientMessage<'SharedClientMessage>.Connect state.User)

                    | InternalUI.SetState newState -> { state with UIState = newState }, None

                    | InternalUI.AddUser user ->
                        (if user.Id = state.User.Id then
                            { state with Connection = Connected }
                         else
                             { state with Users = user :: state.Users }),
                        None

                    | InternalUI.RemoveUser user ->
                        { state with
                            Users =
                                state.Users
                                |> List.filter (fun { Id = id } -> id <> user.Id)
                        },
                        None

                    | InternalUI.SharedServerMessage msg ->
                        let (sharedState, cmd) = handler msg state.UIState

                        let cmd =
                            cmd
                            |> Option.map
                                InternalUI.InternalClientMessage<'SharedClientMessage>
                                    .SharedClientMessage

                        { state with UIState = sharedState }, cmd

            //  printfn "Update completed. Cmd: %A \nState: %s" cmd (JS.JSON.stringify newState)

            newState,
            match cmd with
            | None -> Cmd.none
            | Some cmd -> Cmd.bridgeSend cmd

        let view =
            let mainComponent = FunctionComponent.Lazy (lazyView, div [] [ str "Loading..." ])

            let mainComponentFactory (state: PrivateState<'UIState>)
                                     (serverToClientDispatch: PrivateServerMessage<'SharedServerMessage, 'UIState> -> unit)
                                     =

                mainComponent
                    {
                        UIState = state.UIState
                        PrivateState = state
                        ServerToClientDispatch = serverToClientDispatch
                    }

            mainComponentFactory

        Program.mkProgram init update view
        |> fun program ->
            if not bridge then
                program
            else
                program
                |> Program.withBridgeConfig
//                    (Bridge.endpoint $"{Bridge.Endpoints.apiBaseUrl}{Bridge.Endpoints.socketPath}"
                    (Bridge.endpoint Bridge.Endpoints.socketPath
//                     |> Bridge.withUrlMode Raw
                     |> Bridge.withMapping InternalServerMessage
                     |> Bridge.withWhenDown ConnectionLost)
        |> Program.withReactSynchronous "root"
#if DEBUG
        //  |> Program.withConsoleTrace
        //  |> Program.withDebugger
#endif
        |> Program.run
