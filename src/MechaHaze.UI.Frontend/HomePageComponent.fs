namespace MechaHaze.UI.Frontend

open Browser.Types
open MechaHaze.UI.Frontend
open MechaHaze.Shared
open Fable.Core
open Fable.FontAwesome
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open MechaHaze.UI
open System


module HomePageComponent =

    type Props =
        {
            Dispatch: SharedState.SharedServerMessage -> unit
            UIState: UIState.State
            PrivateState: Client.PrivateState<UIState.State>
        }

    type State =
        {
            SelectedPeaks: Set<string * string>
        }
        static member inline Default = { SelectedPeaks = Set.empty }

    type ToggleBindingSource = ToggleBindingSource of string * string


    let ``default`` =
        FunctionComponent.Of
            ((fun props ->
                let state =
                    Hooks.useReducer
                        ((fun state dispatch ->
                            match dispatch with
                            | ToggleBindingSource (bindingSource, layer) ->
                                let id = bindingSource, layer

                                { state with
                                    SelectedPeaks = state.SelectedPeaks |> Set.toggle id
                                }),
                         State.Default)

                let events =
                    {|
                        OnMenuBindingSourcePeakClick =
                            fun bindingSource layer _ -> state.update (ToggleBindingSource (bindingSource, layer))
                        OnMenuDebugClick =
                            fun _ -> props.Dispatch (SharedState.ClientSetDebug (not props.UIState.SharedState.Debug))
                        OnBindingsDestinationToggle =
                            fun id -> props.Dispatch (SharedState.ClientToggleBinding (Bindings.Binding ("", id)))
                        OnPresetToggle = fun presetName -> props.Dispatch (SharedState.ClientTogglePreset presetName)
                        OnActiveBindingsPresetChange =
                            fun presetName -> props.Dispatch (SharedState.ClientSetActiveBindingsPreset presetName)
                        OnMenuTopRightButtonClick = fun _ -> printfn "CLICK"
                        OnOffsetSliderChange =
                            fun (e: Event) -> props.Dispatch (SharedState.ClientSetOffset (float e.Value))
                        OnBindingLink =
                            fun o ->
                                if JS.Constructors.Array.isArray o then
                                    match List.ofArray (box o :?> string []) with
                                    | a :: b :: _ ->
                                        props.Dispatch (SharedState.ClientToggleBinding (Bindings.Binding (a, b)))
                                    | _ -> ()
                        OnAlignmentAutoLockClick =
                            fun _ ->
                                props.Dispatch (SharedState.ClientSetAutoLock (not props.UIState.SharedState.AutoLock))
                        OnAlignmentRecordingModeClick =
                            fun _ ->
                                props.Dispatch
                                    (SharedState.ClientSetRecordingMode (not props.UIState.SharedState.RecordingMode))
                        OnAlignmentOffsetCenterClick = fun _ -> props.Dispatch (SharedState.ClientSetOffset 0.)
                        OnAlignmentOffsetFlipClick =
                            fun _ ->
                                props.Dispatch (SharedState.ClientSetOffset -props.UIState.SharedState.Track.Offset)
                        OnAlignmentLockClick =
                            fun _ ->
                                props.Dispatch
                                    (SharedState.ClientSetLocked (not props.UIState.SharedState.Track.Locked))
                    |}

                Text.div [
                             Props [ Style [ Height "100%" ] ]
                             Modifiers [
                                 Modifier.TextSize (Screen.All, TextSize.Is7)
                             ]
                         ] [

                    if not props.UIState.SharedState.Debug then
                        PageLoader.pageLoader [
                                                  PageLoader.Color IsDark
                                                  PageLoader.IsActive
                                                      (match props.PrivateState.Connection with
                                                       | Client.Connected _ -> false
                                                       | _ -> true)
                                              ] []

                    Navbar.navbar [
                                      Navbar.Color IsBlack
                                      Navbar.Props [
                                          Style [ Height 36; MinHeight 36 ]
                                      ]
                                  ] [

                        Navbar.Item.div [
                                            Navbar.Item.HasDropdown
                                            Navbar.Item.IsHoverable
                                        ] [

                            Navbar.Link.div [] [
                                Element.icon Fa.Solid.Bars ""
                            ]

                            Navbar.Dropdown.div [] [

                                Navbar.Item.div [] [

                                    div [
                                            ClassName "field"
                                            OnClick events.OnMenuDebugClick
                                        ] [

                                        Checkbox.input [
                                            CustomClass "switch is-small is-dark"
                                            Props [
                                                Checked props.UIState.SharedState.Debug
                                                OnChange (fun _ -> ())
                                            ]
                                        ]

                                        Checkbox.checkbox [] [ str "Debug" ]
                                    ]
                                ]

                                Navbar.divider [] []

                                [
                                    Bindings.sources.Levels
                                    Bindings.sources.Pitch
                                ]
                                |> List.map (fun bindingSource ->
                                    div [
                                            Key bindingSource
                                            Class "navbar-nested-dropdown"
                                        ] [

                                        Navbar.Item.div [] [
                                            str
                                                (bindingSource.[0].ToString().ToUpper()
                                                 + bindingSource.Substring 1)
                                            Icon.icon [
                                                          Icon.Props [
                                                              Style [ Float FloatOptions.Right ]
                                                          ]
                                                      ] [
                                                Fa.i [
                                                         Fa.Solid.ChevronRight
                                                     ] []
                                            ]
                                        ]

                                        Dropdown.menu [] [
                                            Navbar.Dropdown.div [] [

                                                Bindings.layers
                                                |> List.map (fun (layer, _) ->
                                                    let id = bindingSource, layer

                                                    Navbar.Item.div [
                                                                        Navbar.Item.Props [
                                                                            Key layer
                                                                            OnClick
                                                                                (events.OnMenuBindingSourcePeakClick
                                                                                    bindingSource
                                                                                     layer)
                                                                        ]
                                                                    ] [

                                                        Checkbox.input [
                                                            CustomClass "switch is-small is-dark"
                                                            Props [
                                                                Checked (state.current.SelectedPeaks |> Set.contains id)
                                                                OnChange (fun _ -> ())
                                                            ]
                                                        ]

                                                        Checkbox.checkbox [] [ str layer ]
                                                    ])
                                                |> ofList
                                            ]
                                        ]
                                    ])
                                |> ofList


                                Navbar.divider [] []

                                Navbar.Item.a [] [ str "X" ]

                                Navbar.Item.a [] [ str "Y" ]

                                Navbar.divider [] []

                                Navbar.Item.a [] [ str "Z" ]
                            ]
                        ]

                        Navbar.End.div [] [

                            Navbar.Item.div [] [

                                Button.button [
                                                  Button.Size IsSmall
                                                  Button.OnClick events.OnMenuTopRightButtonClick
                                              ] [
                                    str "X"
                                ]
                            ]
                        ]
                    ]

                    if props.UIState.SharedState.Track.Id <> "" then
                        PeaksComponent.``default``
                            {
                                Track = props.UIState.SharedState.Track
                                Debug = props.UIState.SharedState.Debug
                                Layer = "all"
                                BindingSource = Bindings.sources.Levels
                            }

                    div [
                            Style [
                                Position PositionOptions.Relative
                            ]
                        ] [

                        div [
                                Style [
                                    Position PositionOptions.Absolute
                                    Left "50%"
                                    Top 0
                                    BackgroundColor "#aaa"
                                    Opacity "60%"
                                    Width 1
                                    Height "100%"
                                ]
                            ] []

                        Slider.slider [
                            Slider.IsFullWidth
                            Slider.Color IsDark
                            Slider.Props [
                                Style [ Margin "5px 0" ]
                            ]
                            Slider.Min -0.5
                            Slider.Max 0.5
                            Slider.Value (props.UIState.SharedState.Track.Offset)
                            Slider.Step (1. / 1000.)
                            Slider.Disabled (not props.UIState.SharedState.Track.Locked)
                            Slider.OnChange events.OnOffsetSliderChange
                        ]
                    ]

                    Columns.columns [
                                        Columns.Props [
                                            Style [
                                                Margin -5
                                                CSSProp.Overflow OverflowOptions.Auto
                                            ]
                                        ]
                                    ] [

                        Column.column [
                                          Column.Width (Screen.All, Column.Is4)
                                          Column.Props [
                                              Style [
                                                  MinWidth 855
                                                  Position PositionOptions.Relative
                                              ]
                                          ]
                                      ] [

                            (* Tab 1.1 *)
                            Tabs.tabs [
                                          Tabs.Size IsSmall
                                          Tabs.Props [ Style [ MarginBottom 10 ] ]
                                      ] [

                                Tabs.tab [
                                             Tabs.Tab.IsActive true
                                         ] [
                                    a [] [ str "Bindings" ]
                                ]
                            ]

                            div [
                                    Style [
                                        Position PositionOptions.Absolute
                                        Top 15
                                        Right 4
                                    ]
                                ] [

                                Dropdown.dropdown [
                                                      Dropdown.IsHoverable
                                                  ] [
                                    div [] [
                                        Element.icon Fa.Solid.Cog ""
                                        Element.icon Fa.Solid.AngleDown ""
                                    ]
                                    Dropdown.menu [] [
                                        Dropdown.content [
                                                             Props [ Style [ Padding "15px 20px" ] ]
                                                         ] [

                                            BindingsSettingsDropdownComponent.``default``
                                                {
                                                    BindingsPresetMap = props.UIState.SharedState.BindingsPresetMap
                                                    ActiveBindingsPreset =
                                                        props.UIState.SharedState.ActiveBindingsPreset
                                                    TogglePreset = events.OnPresetToggle
                                                    ToggleBindingsDestination = events.OnBindingsDestinationToggle
                                                    SetActiveBindingsPreset = events.OnActiveBindingsPresetChange
                                                }
                                        ]
                                    ]
                                ]
                            ]

                            div [
                                    Class "storm-diagram-container"
                                    Style [
                                        BackgroundColor "#151515"
                                        Height 800
                                        Width 830
                                    ]
                                ] [

                                StormDiagramComponent.``default``
                                    {
                                        BindingsPreset =
                                            props.UIState.SharedState.BindingsPresetMap
                                            |> Bindings.ofPresetMap
                                            |> Map.tryFind props.UIState.SharedState.ActiveBindingsPreset
                                            |> Option.defaultValue (Bindings.Preset [])

                                        OnLink = events.OnBindingLink
                                    }
                            ]
                        ]

                        Column.column [] [

                            (* Tab 2.1 *)
                            Tabs.tabs [
                                          Tabs.Size IsSmall
                                          Tabs.Props [ Style [ MarginBottom 10 ] ]
                                      ] [

                                Tabs.tab [
                                             Tabs.Tab.IsActive true
                                         ] [
                                    a [] [ str "Time Sync" ]
                                ]
                            ]

                            let data =
                                props.UIState.TimeSyncMap
                                |> SharedState.ofTimeSyncMap
                                |> Map.values
                                |> Seq.filter (fun { Offsets = offsets } -> offsets |> Array.isEmpty |> not)
                                |> Seq.sortByDescending (fun { Offsets = offsets } ->
                                    offsets
                                    |> Seq.last
                                    |> SharedState.ofTimeSyncOffset
                                    |> fst)
                                |> Seq.tryHead
                                |> function
                                | None -> [||]
                                | Some timeSync ->
                                    let _, y =
                                        timeSync.Offsets
                                        |> Array.map SharedState.ofTimeSyncOffset
                                        |> Array.unzip

                                    let x, y' =
                                        timeSync.StableOffsets
                                        |> Array.map SharedState.ofTimeSyncOffset
                                        |> Array.unzip

                                    [|
                                        [|
                                            x
                                            y
                                        |]
                                        [|
                                            x
                                            y'
                                        |]
                                    |]


                            JitterChartComponent.``default`` { Data = data }
                        ]

                        Column.column [
                                          Column.Width (Screen.All, Column.Is2)
                                      ] [

                            (* Tab 3.1 *)
                            Tabs.tabs [
                                          Tabs.Size IsSmall
                                          Tabs.Props [ Style [ MarginBottom 10 ] ]
                                      ] [

                                Tabs.tab [
                                             Tabs.Tab.IsActive true
                                         ] [
                                    a [] [ str "Manual Alignment" ]
                                ]
                            ]

                            Columns.columns [] [

                                Column.column [] [
                                    str "Offset: "
                                    ofFloat props.UIState.SharedState.Track.Offset
                                ]

                                Column.column [
                                                  Column.Props [ Style [ PaddingTop 17 ] ]
                                              ] [

                                    div [
                                            ClassName "field"
                                            OnClick events.OnAlignmentAutoLockClick
                                        ] [

                                        Checkbox.input [
                                            CustomClass "switch is-small is-dark"
                                            Props [
                                                Checked props.UIState.SharedState.AutoLock
                                                OnChange (fun _ -> ())
                                            ]
                                        ]
                                        Checkbox.checkbox [] [ str "Auto lock" ]
                                    ]

                                    br []

                                    div [
                                            ClassName "field"
                                            OnClick events.OnAlignmentRecordingModeClick
                                        ] [

                                        Checkbox.input [
                                            CustomClass "switch is-small is-dark"
                                            Props [
                                                Checked props.UIState.SharedState.RecordingMode
                                                OnChange (fun _ -> ())
                                            ]
                                        ]
                                        Checkbox.checkbox [] [ str "Auto Deaf" ]
                                    ]
                                ]
                            ]

                            Columns.columns [] [

                                Column.column [] [

                                    Button.button [
                                                      Button.Size IsSmall
                                                      Button.Color IsDark
                                                      Button.Disabled (not props.UIState.SharedState.Track.Locked)
                                                      Button.OnClick events.OnAlignmentOffsetCenterClick
                                                  ] [

                                        Element.icon Fa.Solid.AlignCenter "Center"
                                    ]
                                    str " "
                                    Button.button [
                                                      Button.Size IsSmall
                                                      Button.Color IsDark
                                                      Button.Disabled (not props.UIState.SharedState.Track.Locked)
                                                      Button.OnClick events.OnAlignmentOffsetFlipClick
                                                  ] [

                                        Element.icon Fa.Solid.Sync "Flip"
                                    ]
                                ]

                                Column.column [] [

                                    div [
                                            ClassName "field"
                                            OnClick events.OnAlignmentLockClick
                                        ] [

                                        Checkbox.input [
                                            CustomClass "switch is-small is-dark"
                                            Props [
                                                Checked props.UIState.SharedState.Track.Locked
                                                OnChange (fun _ -> ())
                                            ]
                                        ]
                                        Checkbox.checkbox [] [ str "Locked" ]
                                    ]
                                ]
                            ]

                            (* Tab 3.2 *)
                            Tabs.tabs [
                                          Tabs.Size IsSmall
                                          Tabs.Props [ Style [ MarginBottom 10 ] ]
                                      ] [

                                Tabs.tab [
                                             Tabs.Tab.IsActive true
                                         ] [
                                    a [] [ str "Track History" ]
                                ]
                            ]

                            str "Current Track:"
                            br []

                            match props.UIState.SharedState.Track with
                            | { Id = "" } -> str "- No track"

                            | track ->
                                str "- Id: "
                                str track.Id
                                br []
                                str "- Position: "
                                ofFloat track.Position

                            br []
                            br []

                            if props.UIState.SharedState.Debug then

                                Tabs.tabs [
                                              Tabs.Size IsSmall
                                              Tabs.Props [ Style [ MarginBottom 10 ] ]
                                          ] [

                                    Tabs.tab [
                                                 Tabs.Tab.IsActive true
                                             ] [
                                        a [] [ str "Debug Info" ]
                                    ]
                                ]

                                str "User Id: "
                                str props.PrivateState.User.Id
                                br []
                                str "Connection: "
                                str $"%A{props.PrivateState.Connection}"
                                br []

                                br []

                                str "Users: "

                                match props.PrivateState.Users with
                                | [] -> str "None"
                                | _ ->
                                    props.PrivateState.Users
                                    |> List.map (fun user -> user.Id)
                                    |> List.sort
                                    |> List.map (fun x -> div [] [ str x ])
                                    |> div []

                                    br []
                        ]
                    ]
                ]),
             memoizeWith = equalsButFunctions)
