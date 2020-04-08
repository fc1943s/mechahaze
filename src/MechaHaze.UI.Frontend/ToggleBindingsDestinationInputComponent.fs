namespace MechaHaze.UI.Frontend

open Browser.Types
open MechaHaze.Shared
open Fable.React
open Fable.React.Props
open Fulma
open Suigetsu.Core

module ToggleBindingsDestinationInputComponent =

    type Props =
        { ToggleBindingsDestination: string -> unit
          Options: Input.Option list
          Style: CSSProp list }

    type State =
        { Text: string
          Error: bool }
        static member inline Default =
            { Text = ""
              Error = false }
        
    type Message =
        | SetError of bool
        | SetText of string
        
    let ``default`` = FunctionComponent.Of (fun props ->
        let state =
            Hooks.useReducer ((fun state dispatch ->
                match dispatch with
                | SetError error -> { state with Error = error }
                | SetText text -> { state with Text = text }
            ), State.Default)
            
        let events = {|
            OnKeyDown = fun (e: KeyboardEvent) ->
                if e.key = "Enter" then
                    let valid =
                        [ Bindings.destinations.Resolume
                          Bindings.destinations.Magic ]
                        |> Seq.map (flip (+) (string Bindings.separator))
                        |> Seq.filter state.current.Text.StartsWith
                        |> Seq.map String.length
                        |> Seq.exists ((>) state.current.Text.Length)

                    if valid then
                        props.ToggleBindingsDestination state.current.Text
                        state.update (SetText "")
                        state.update (SetError false)
                    else
                        state.update (SetError true)
                                                                     
            OnChange = fun (e: Event) ->
                state.update (SetText e.Value)
        |}

        Input.text
            (props.Options @ [ Input.Value state.current.Text
                               Input.Props [ OnKeyDown events.OnKeyDown
                                             OnChange events.OnChange
                                             Style (props.Style @ (if state.current.Error then [ Color "#F33" ] else [])) ] ] )
    , memoizeWith = equalsButFunctions)
