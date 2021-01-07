namespace MechaHaze.UI.Frontend

open Browser.Types
open MechaHaze.Shared
open Fable.React
open Fable.React.Props
open Fulma
open MechaHaze.Shared.Bindings

module AddBindingsPresetInputComponent =

    type Props =
        {
            BindingsPresetMap: Bindings.PresetMap
            AddPreset: PresetId -> unit
            Options: Input.Option list
            Style: CSSProp list
        }

    type State =
        {
            Text: string
            Error: bool
        }
        static member inline Default = { Text = ""; Error = false }

    type Message =
        | SetError of bool
        | SetText of string

    let ``default`` =
        FunctionComponent.Of
            ((fun props ->
                let state =
                    Hooks.useReducer
                        ((fun state dispatch ->
                            match dispatch with
                            | SetError error -> { state with Error = error }
                            | SetText text -> { state with Text = text }),
                         State.Default)

                let events =
                    {|
                        OnKeyDown =
                            fun (e: KeyboardEvent) ->
                                if e.key = "Enter" then
                                    let valid =
                                        props.BindingsPresetMap
                                        |> ofPresetMap
                                        |> Map.exists (fun (PresetId presetId) _ -> presetId = state.current.Text)
                                        |> not

                                    if valid && state.current.Text.Length > 0 then
                                        props.AddPreset (PresetId state.current.Text)
                                        state.update (SetText "")
                                        state.update (SetError false)
                                    else
                                        state.update (SetError true)
                        OnChange = fun (e: Event) -> state.update (SetText e.Value)
                    |}

                Input.text
                    (props.Options
                     @ [
                         Input.Value state.current.Text
                         Input.Props [
                             OnKeyDown events.OnKeyDown
                             OnChange events.OnChange
                             Style
                                 (props.Style
                                  @ (if state.current.Error then
                                      [
                                          Color "#F33"
                                      ]
                                     else
                                         []))
                         ]
                     ])),
             memoizeWith = equalsButFunctions)
