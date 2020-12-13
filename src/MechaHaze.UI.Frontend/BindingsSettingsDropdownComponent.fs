namespace MechaHaze.UI.Frontend

open Fable.FontAwesome
open FSharpPlus
open Fable.React
open Fable.React.Props
open Fulma
open MechaHaze.Shared

module BindingsSettingsDropdownComponent =

    type Props =
        {
            BindingsPresetMap: Bindings.PresetMap
            ActiveBindingsPreset: string
            TogglePreset: string -> unit
            ToggleBindingsDestination: string -> unit
            SetActiveBindingsPreset: string -> unit
        }

    type Tab =
        | PresetList
        | PresetAdd


    let confirmationModal (props: ModalManager.PrivateManager) =
        let onToggle = fun _ -> props.Toggle ()
        let onConfirm = fun _ -> props.Confirm ()

        Modal.modal [
                        Modal.IsActive props.Visible
                    ] [

            Modal.background [
                                 Props [ OnClick onToggle ]
                             ] []

            Modal.Card.card [] [
                Modal.Card.head [] [
                    Modal.Card.title [] [
                        str "Confirmation"
                    ]

                    Delete.delete [
                                      Delete.OnClick onToggle
                                  ] []
                ]

                Modal.Card.body [] [
                    str "Are you sure you want to delete this preset?"
                ]

                Modal.Card.foot [] [
                    Button.button [
                                      Button.Color IsDanger
                                      Button.OnClick onConfirm
                                  ] [
                        str "Delete"
                    ]
                    Button.button [
                                      Button.OnClick onToggle
                                  ] [
                        str "Cancel"
                    ]
                ]
            ]
        ]

    let ``default`` =
        FunctionComponent.Of
            ((fun props ->
                let tabManager = TabManager.create<Tab> PresetList
                let modalManager = ModalManager.create confirmationModal

                let events =
                    {|
                        OnPresetDeleteClick =
                            fun presetName _ -> modalManager.Confirm (fun _ -> props.TogglePreset presetName)
                        OnPresetClick = fun presetName _ -> props.SetActiveBindingsPreset presetName
                        OnPresetAdd =
                            fun presetName ->
                                tabManager.SetTab PresetList
                                props.TogglePreset presetName
                    |}


                div [] [
                    modalManager.Element

                    Tabs.tabs [
                                  Tabs.Size IsSmall
                                  Tabs.Props [ Style [ MarginBottom 10 ] ]
                              ] [

                        Tabs.tab [
                                     tabManager.IsActive PresetList
                                 ] [
                            a [
                                tabManager.CreateOnClick PresetList
                              ] [
                                str "Presets"
                            ]
                        ]

                        Tabs.tab [
                                     tabManager.IsActive PresetAdd
                                 ] [
                            a [
                                tabManager.CreateOnClick PresetAdd
                              ] [
                                str "+"
                            ]
                        ]
                    ]

                    div [
                            Style [
                                tabManager.CreateDisplay PresetList
                            ]
                        ] [


                        props.BindingsPresetMap
                        |> Bindings.ofPresetMap
                        |> Map.keys
                        |> Seq.map (fun presetName ->
                            let active = presetName = props.ActiveBindingsPreset

                            Control.div [
                                            Control.Props [
                                                Key presetName
                                                Style [ MarginBottom 5 ]
                                            ]
                                        ] [

                                Tag.list [
                                             Tag.List.HasAddons
                                         ] [

                                    Tag.tag [] [
                                        if active then
                                            span [
                                                     Style [ Color "#fff" ]
                                                 ] [
                                                str presetName
                                            ]
                                        else
                                            a [
                                                OnClick (events.OnPresetClick presetName)
                                              ] [
                                                str presetName
                                            ]
                                    ]

                                    if not active then
                                        Tag.delete [
                                                       Tag.Props [
                                                           OnClick (events.OnPresetDeleteClick presetName)
                                                       ]
                                                   ] []
                                ]
                            ])
                        |> Seq.toList
                        |> ofList

                        Dropdown.divider []

                        Field.div [] [
                            Field.label [] [
                                str "Toggle Destination"
                            ]

                            Control.div [
                                            Control.HasIconLeft
                                        ] [

                                ToggleBindingsDestinationInputComponent.``default``
                                    {
                                        ToggleBindingsDestination = props.ToggleBindingsDestination
                                        Options =
                                            [
                                                Input.Placeholder ""
                                                Input.Size IsSmall
                                            ]
                                        Style =
                                            [
                                                Width 400
                                            ]
                                    }

                                Icon.icon [
                                              Icon.Size IsSmall
                                              Icon.IsLeft
                                          ] [
                                    Fa.i [
                                             Fa.Solid.ExchangeAlt
                                         ] []
                                ]
                            ]
                        ]
                    ]

                    div [
                            Style [
                                tabManager.CreateDisplay PresetAdd
                            ]
                        ] [

                        Field.div [] [
                            Field.label [] [ str "Add Preset" ]

                            Control.div [
                                            Control.HasIconLeft
                                        ] [

                                AddBindingsPresetInputComponent.``default``
                                    {
                                        BindingsPresetMap = props.BindingsPresetMap
                                        AddPreset = events.OnPresetAdd
                                        Options =
                                            [
                                                Input.Placeholder ""
                                                Input.Size IsSmall
                                            ]
                                        Style =
                                            [
                                                Width 400
                                            ]
                                    }

                                Icon.icon [
                                              Icon.Size IsSmall
                                              Icon.IsLeft
                                          ] [

                                    Fa.i [
                                             Fa.Solid.Plus
                                         ] []
                                ]
                            ]
                        ]
                    ]
                ]),
             memoizeWith = equalsButFunctions)
