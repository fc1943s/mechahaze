namespace MechaHaze.UI.Backend.ElmishBridge

open Elmish.Bridge
open Elmish
open Serilog
open Giraffe.SerilogExtensions
open MechaHaze.Core
open MechaHaze.UI

module Server =

    type StateScope<'UIState> =
        | Internal of 'UIState
        | Remote of 'UIState

    let scopeToState = function
        | Internal state -> state
        | Remote state -> state

    type PrivateClientMessage<'SharedClientMessage> =
        | InternalClientMessage of InternalUI.InternalClientMessage<'SharedClientMessage>
        | Closed

    type ConnectedPrivateState<'UIState> =
        { User: InternalUI.User
          OldUIState: 'UIState }

    type PrivateState<'UIState> =
        | Connected of ConnectedPrivateState<'UIState>
        | Disconnected

    type ServerToClientBridge<'UIState, 'SharedServerMessage, 'SharedClientMessage> () =
        let connections = ServerHub<PrivateState<'UIState>,
                                    PrivateClientMessage<'SharedClientMessage>,
                                    InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>> ()

        member _.Connections = connections


        member this.InternalBroadcastToClients (message: InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>,
                                                ?excludeUser: InternalUI.User) =
            message
            |> this.Connections.SendClientIf (fun state ->
                   match state, excludeUser with
                   | Connected c, Some excludeUser when c.User.Id = excludeUser.Id -> false
                   | _ -> true)

        member this.SharedBroadcastToClients (message: 'SharedServerMessage, ?excludeUser: InternalUI.User) =
            let message = InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>.SharedServerMessage message

            match excludeUser with
            | None -> this.InternalBroadcastToClients message
            | Some excludeUser -> this.InternalBroadcastToClients (message, excludeUser)

        member this.GetConnectedUsers () =
            this.Connections.GetModels ()
            |> Seq.choose (function Disconnected -> None | Connected { User = user } -> Some user)
            |> Seq.toList

    let createRouter<'UIState, 'SharedServerMessage, 'SharedClientMessage when 'UIState : equality>
            (stateQueue: SafeQueue.SafeQueue<StateScope<'UIState>>)
            (connections: ServerToClientBridge<'UIState, 'SharedServerMessage, 'SharedClientMessage>)
            (handler: 'SharedClientMessage ->
                      'UIState ->
                      (InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState> -> unit) ->
                      'UIState * 'SharedClientMessage option) =

        let inline update serverToClientDispatch msg state =
            Log.Verbose ("Message from client. Msg: {Msg} State: {State}", msg, state)

            try
                match msg with
                | Closed ->
                    match state with
                    | Disconnected ->
                        ()

                    | Connected state ->
                        Log.Debug ("Closed -> Connected user. Broadcasting RemoveUser id")
                        connections.InternalBroadcastToClients (InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>.RemoveUser state.User)

                    Disconnected, Cmd.none

                | InternalClientMessage msg ->
                    match state, msg with
                    | _, InternalUI.Connect user ->
                        connections.InternalBroadcastToClients (InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>.AddUser user)
                        let state =
                            stateQueue.Dequeue ()
                            |> scopeToState

                        Connected { User = user; OldUIState = state }, Cmd.none

                    | Connected state, InternalUI.SharedClientMessage msg ->
                        let sharedState =
                            stateQueue.Dequeue ()
                            |> scopeToState

                        let newState, cmd = handler msg sharedState serverToClientDispatch

                        let cmd =
                            match cmd with
                            | None -> Cmd.none
                            | Some message -> message |> InternalUI.SharedClientMessage |> InternalClientMessage |> Cmd.ofMsg

                        if sharedState <> newState then
                            stateQueue.Enqueue (Remote newState)

                        Connected { state with OldUIState = newState }, cmd

                    | Disconnected, _ ->
                        state, Cmd.none
            with ex ->
                Log.Error (ex, "Error on server message update"); state, Cmd.none


        let inline init (serverToClientDispatch: Dispatch<InternalUI.InternalServerMessage<'SharedServerMessage, 'UIState>>) () =
            Log.Information ("New client connected. Sending welcome...")

            let state =
                stateQueue.Dequeue ()
                |> scopeToState

            serverToClientDispatch (InternalUI.WelcomeUser (connections.GetConnectedUsers (), state))

            Disconnected, Cmd.none


        Bridge.mkServer InternalUI.socketPath init update
    //  |> Bridge.withConsoleTrace
        |> Bridge.whenDown Closed
        |> Bridge.withServerHub connections.Connections
        |> Bridge.run Giraffe.server
        |> SerilogAdapter.Enable


