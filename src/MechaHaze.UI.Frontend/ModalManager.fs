namespace MechaHaze.UI.Frontend

open Fable.Core
open Fable.React

module ModalManager =

    type State =
        { Visible: bool
          ConfirmCallback: unit -> unit }
        static member inline Default =
            { Visible = false
              ConfirmCallback = fun _ -> () }

    type Message =
        | Toggle
        | ConfirmRequest of (unit -> unit)
        | Confirm
        
    type Manager =
        { Element: ReactElement
          Toggle: unit -> unit
          Confirm: (unit -> unit) -> unit }
        
    type PrivateManager =
        { Visible: bool
          Toggle: unit -> unit
          Confirm: unit -> unit }
        
        
    let create (modalFactory: PrivateManager -> ReactElement) =
        let state =
            Hooks.useReducer (fun state dispatch ->
                
                match dispatch with
                | Toggle ->
                    { State.Default with Visible = not state.Visible }
                    
                | ConfirmRequest callback ->
                    { Visible = true
                      ConfirmCallback = callback }
                    
                | Confirm ->
                    JS.setTimeout (fun _ ->
                        state.ConfirmCallback ()
                    ) 0 |> ignore
                    State.Default
                    
            , State.Default)
            
        let privateManager =
            { Confirm = fun () -> state.update Confirm
              Toggle = fun () -> state.update Toggle
              Visible = state.current.Visible }
            
        let element = modalFactory privateManager
        
        { Element = element
          Toggle = privateManager.Toggle
          Confirm = ConfirmRequest >> state.update }
