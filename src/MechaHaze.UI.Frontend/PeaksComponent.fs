namespace MechaHaze.UI.Frontend

open Browser
open MechaHaze.UI.Frontend
open MechaHaze.Shared
open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
open System
open MechaHaze.UI.Interop.JavaScript


module PeaksComponent =

    type Props =
        {
            Track: SharedState.Track
            Layer: string
            BindingSource: string
            Debug: bool
        }

    type State =
        {
            PeaksHandle: obj
            LastPosition: float
            LastId: string
        }

    let view =
        FunctionComponent.Of
            ((fun (__initialProps: Props) ->
                let state =
                    Hooks.useState
                        {
                            PeaksHandle = null
                            LastPosition = 0.
                            LastId = ""
                        }

                state |> ignore
                str ""),
             memoizeWith = equalsButFunctions)

    type PeaksComponent (initialProps) =
        inherit PureComponent<Props, State>(initialProps)

        let id = Random().Next()

        let ids =
            {|
                waveformContainer = $"waveform-container-{id}"
                zoomviewContainer = $"zoomview-container-{id}"
                overviewContainer = $"overview-container-{id}"
                audio = $"audio-{id}"
            |}

        do
            base.setInitState
                {
                    PeaksHandle = null
                    LastPosition = 0.
                    LastId = ""
                }

        member this.GetTrackMediaUrl () =
            let seconds =
                [
                    5
                    10
                    15
                    30
                    60
                    80
                ]

            let duration = this.props.Track.DurationSeconds / 60.

            let newSeconds =
                seconds
                |> List.tryFind (fun x -> float x > duration)
                |> Option.defaultValue (seconds |> List.last)

            JS.encodeURIComponent $"root/dataset-mp3silence/{newSeconds}.mp3"

        member this.GetTrackDataUri () =
            if this.props.Track.Id = "" then
                JS.undefined
            else
                {|
                    arraybuffer =
                        JS.encodeURIComponent
                            $"root/db-tracks/{this.props.Track.Id}/{this.props.Track.Id}.{this.props.Layer}.peaks.{this.props.BindingSource}.dat"
                |}





        member this.Init () =
            if this.state.PeaksHandle = null
               && this.props.Track.Id <> "" then
                let peaksOptions =
                    {|
                        containers =
                            {|
                                overview = document.getElementById ids.overviewContainer
                                zoomview = document.getElementById ids.zoomviewContainer
                            |}
                        mediaElement = document.getElementById ids.audio
                        dataUri = this.GetTrackDataUri ()
                        zoomLevels =
                            [|
                                256
                            |]
                            |> JavaScript.newJsArray
                        segments =
                            [|
                                {| startTime = 30; endTime = 60; color = "#333" |}
                            |]
                        points =
                            [|
                                {| time = 5; color = "#BBB" |}
                            |]
                        keyboard = false
                        height = 100
                        overviewHighlightColor = "#999"
                        overviewHighlightOffset = 0
                        playheadColor = "#999"
                        axisGridlineColor = "#999"
                        axisLabelColor = "#444"
                        overviewWaveformColor = "#444"
                        showPlayheadTime = true
                        playheadTextColor = "#777"
                        zoomWaveformColor = "#777"
                    |}


                let onPeaksReady error peaksHandle =
                    if error <> null then
                        printfn "ERROR PEAKS %A" error
                    else
                        this.setState (fun state _ -> { state with PeaksHandle = peaksHandle })

                        let zoomview = peaksHandle?views?getView "zoomview"
                        let overview = peaksHandle?views?getView "overview"

                        zoomview?setAmplitudeScale 0.95
                        overview?setAmplitudeScale 0.95


                Ext.peaks.init peaksOptions (System.Action<_, _> onPeaksReady)
                |> ignore



        override this.render () =
            div [] [

                div [
                        Id ids.waveformContainer
                        Style [
                            Position PositionOptions.Relative
                            PointerEvents
                                (if (this.props.Debug) then
                                    "auto"
                                 else
                                     "none")
                        ]
                    ] [

                    audio [
                              Id ids.audio
                              Style [
                                  Position PositionOptions.Absolute
                                  Right 0
                                  Top 100
                                  Zoom 0.8
                                  ZIndex 1000
                              ]
                              Controls this.props.Debug
                          ] [

                        source [
                            HTMLAttr.Type "audio/mpeg"
                            Src (this.GetTrackMediaUrl ())
                        ]
                    ]

                    div [
                            Id ids.zoomviewContainer
                        ] []
                    div [
                            Id ids.overviewContainer
                        ] []
                ]
            ]

        override this.componentDidMount () =
            printfn "PEAKS DIDMOUNT"

            this.Init ()

        override this.componentWillUnmount () =
            printfn "PEAKS WILLUNMOUNT"

            if this.state.PeaksHandle <> null then
                this.state.PeaksHandle?destroy ()

        override this.componentDidUpdate (__prevProps, ___prevState) =
            //  printfn "PEAKS DIDUPDATE !%A! !%A! !%A! !%A! !%A!" this.state.LastId this.props.Track.Id this.state.PeaksHandle this.props.Track.Position this.state.LastPosition

            this.Init ()

            if this.state.PeaksHandle <> null then

                let player = this.state.PeaksHandle?player

                if this.props.Track.Position > 0. then
                    if this.state.LastPosition
                       <> this.props.Track.Position then
                        if this.state.LastId = ""
                           || this.state.LastId <> this.props.Track.Id then
                            let props =
                                {|
                                    dataUri = this.GetTrackDataUri ()
                                    mediaUrl = this.GetTrackMediaUrl ()
                                |}

                            let onError error =
                                if error <> null then
                                    printfn "SETSOURCE ERROR: %A" error

                            this.state.PeaksHandle?setSource (props, onError)

                        this.setState (fun state _ ->
                            { state with
                                LastPosition = this.props.Track.Position
                                LastId = this.props.Track.Id
                            })

                        let diff =
                            (float (DateTime.UtcNow.Ticks - this.props.Track.Timestamp)
                             / 10000.
                             / 1000.)
                            - this.props.Track.Offset

                        printfn "diff %A" diff
                        player?seek (this.props.Track.Position + diff)

                        if not (player?isPlaying ()) then
                            player?play ()

                else
                    player?pause ()



    let ``default`` props = ofType<PeaksComponent, _, _> props []

    let default2 props =
        reactiveCom (fun __props -> 0) (fun __message __state -> 0) (fun __model __dispatch -> str "") "" props []
