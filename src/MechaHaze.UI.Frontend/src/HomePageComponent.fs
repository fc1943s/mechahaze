namespace MechaHaze.UI.Frontend

open FSharpPlus
open Browser.Types
open Feliz.Recoil.Bridge
open Elmish.Bridge
open Feliz
open Feliz.Recoil
open MechaHaze.UI.Frontend
open MechaHaze.Shared
open MechaHaze.Shared.Core
open Fable.Core
open Fable.FontAwesome
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open MechaHaze.UI
open System
open MechaHaze.UI.Frontend.ElmishBridge


module HomePageComponent =

    type Props =
        {
            Dispatch: SharedState.Response -> unit
            UIState: UIState.State
            PrivateState: Client.PrivateState<UIState.State>
        }

    let ``default`` =
        FunctionComponent.Of
            ((fun () ->
                let uiState, setUiState = Recoil.useState Atoms.uiState
                let debug, setDebug = Recoil.useState Atoms.debug
                let trackId, setTrackId = Recoil.useState Atoms.trackId
                let locked, setLocked = Recoil.useState (AtomFamilies.Track.locked trackId)
                let offset, setOffset = Recoil.useState (AtomFamilies.Track.offset trackId)
                let position, setPosition = Recoil.useState (AtomFamilies.Track.position trackId)
                let timestamp, setTimestamp = Recoil.useState (AtomFamilies.Track.timestamp trackId)
                let debugInfo, setDebugInfo = Recoil.useState (AtomFamilies.Track.debugInfo trackId)
                let durationSeconds, setDurationSeconds = Recoil.useState (AtomFamilies.Track.durationSeconds trackId)
                let autoLock, setAutoLock = Recoil.useState Atoms.autoLock
                let recordingMode, setRecordingMode = Recoil.useState Atoms.recordingMode
                let activeBindingsPreset, setActiveBindingsPreset = Recoil.useState Atoms.activeBindingsPreset
                let presetIdList, setPresetIdList = Recoil.useState Atoms.presetIdList
                let processIdList, setProcessIdList = Recoil.useState Atoms.processIdList
                let track = Recoil.useValue (SelectorFamilies.track trackId)

                let selectedPeaks, setSelectedPeaks = React.useState (Set.empty: Set<string * string>)

                let events =
                    {|
                        OnMenuBindingSourcePeakClick =
                            fun bindingSource layer _ ->
                                let id = bindingSource, layer
                                setSelectedPeaks (selectedPeaks |> Set.toggle id)
                        OnMenuDebugClick = fun _ -> setDebug (not debug)
                        OnBindingsDestinationToggle =
                            fun id -> Bridge.Send (SharedState.ClientToggleBinding (Bindings.Binding ("", id)))
                        OnPresetToggle = fun presetName -> Bridge.Send (SharedState.ClientTogglePreset presetName)
                        OnActiveBindingsPresetChange =
                            fun presetName ->
                                setActiveBindingsPreset presetName
                                Bridge.Send (SharedState.SetActiveBindingsPreset presetName)
                        OnMenuTopRightButtonClick = fun _ -> printfn "CLICK"
                        OnOffsetSliderChange =
                            fun (e: Event) ->
                                let newOffset = float e.Value
                                setOffset newOffset
                                Bridge.Send (SharedState.SetOffset newOffset)
                        OnBindingLink =
                            fun o ->
                                if JS.Constructors.Array.isArray o then
                                    match List.ofArray (box o :?> string []) with
                                    | a :: b :: _ ->
                                        Bridge.Send (SharedState.ClientToggleBinding (Bindings.Binding (a, b)))
                                    | _ -> ()
                        OnAlignmentAutoLockClick =
                            fun _ ->
                                let newAutoLock = not autoLock
                                setAutoLock newAutoLock
                                Bridge.Send (SharedState.SetAutoLock newAutoLock)
                        OnAlignmentRecordingModeClick =
                            fun _ ->
                                let newRecordingMode = not recordingMode
                                setRecordingMode newRecordingMode
                                Bridge.Send (SharedState.SetRecordingMode newRecordingMode)
                        OnAlignmentOffsetCenterClick =
                            fun _ ->
                                let newOffset = 0.
                                setOffset newOffset
                                Bridge.Send (SharedState.SetOffset newOffset)
                        OnAlignmentOffsetFlipClick =
                            fun _ ->
                                let newOffset = -offset
                                setOffset newOffset
                                Bridge.Send (SharedState.SetOffset newOffset)
                        OnAlignmentLockClick =
                            fun _ ->
                                let newLocked = not locked
                                setLocked newLocked
                                Bridge.Send (SharedState.SetLocked newLocked)
                    |}

                Text.div [
                             Props [ Style [ Height "100%" ] ]
                             Modifiers [
                                 Modifier.TextSize (Screen.All, TextSize.Is7)
                             ]
                         ] [

                    if not debug then
                        PageLoader.pageLoader [
                                                  PageLoader.Color IsDark
                                                  PageLoader.IsActive (processIdList.Length > 0)
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
                                                Checked debug
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
                                                                Checked (selectedPeaks |> Set.contains id)
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

                    if trackId <> SharedState.Track.Default.Id then
                        PeaksComponent.``default``
                            {
                                Track = track
                                Debug = debug
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
                            Slider.Value (track.Offset)
                            Slider.Step (1. / 1000.)
                            Slider.Disabled (not track.Locked)
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
                                                    BindingsPresetMap = uiState.SharedState.BindingsPresetMap
                                                    ActiveBindingsPreset = activeBindingsPreset
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
                                            uiState.SharedState.BindingsPresetMap
                                            |> Bindings.ofPresetMap
                                            |> Map.tryFind activeBindingsPreset
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
                                uiState.TimeSyncMap
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
                                    ofFloat offset
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
                                                Checked autoLock
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
                                                Checked recordingMode
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
                                                      Button.Disabled (not locked)
                                                      Button.OnClick events.OnAlignmentOffsetCenterClick
                                                  ] [

                                        Element.icon Fa.Solid.AlignCenter "Center"
                                    ]
                                    str " "
                                    Button.button [
                                                      Button.Size IsSmall
                                                      Button.Color IsDark
                                                      Button.Disabled (not locked)
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
                                                Checked locked
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

                            if trackId = SharedState.Track.Default.Id then
                                str "- No track"
                            else
                                str "- Id: "
                                let (SharedState.TrackId trackId) = trackId
                                str trackId
                                br []
                                str "- Position: "
                                ofFloat position

                            br []
                            br []

                            if debug then

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
                                //                                str props.PrivateState.User.Id
                                str "???"
                                br []
                                str "Connection: "
                                //                                str $"%A{props.PrivateState.Connection}"
                                str "???"
                                br []

                                br []
                        ]
                    ]
                ]),
             memoizeWith = equalsButFunctions)
