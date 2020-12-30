namespace MechaHaze.UI

module InternalUI =

    type User = { Id: string }

    type InternalServerMessage<'SharedServerMessage, 'UIState> =
        | WelcomeUser of User list * 'UIState
        | AddUser of User
        | RemoveUser of User
        | SetState of 'UIState

        | SharedServerMessage of 'SharedServerMessage

    type InternalClientMessage<'SharedClientMessage> =
        | Connect of User

        | SharedClientMessage of 'SharedClientMessage

