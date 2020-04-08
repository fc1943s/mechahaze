namespace MechaHaze.UI.Frontend

open Fable.Core
open MechaHaze.UI.Frontend
open Fable.React

module JitterChartComponent =

    type Props =
        { Data: float[][][] }

    let ``default`` = FunctionComponent.Of (fun props ->
        
        match props.Data with
        | [||] -> nothing
        | data ->
            let data = [|
                {| ``type`` = "scatter"
                   mode = "markers"
                   name = "jitter"
                   x = data.[0].[0]
                   y = data.[0].[1]
                   marker = {| color = "#444" |} |}
                   
                {| ``type`` = "scatter"
                   mode = "markers"
                   name = "delay"
                   x = data.[1].[0]
                   y = data.[1].[1]
                   marker = {| color = "#AAA" |} |}
            |]
                
            let layout =
                {| autosize = true
                   height = 500
                   xaxis = [ "gridcolor", box "#222" ] |> JsInterop.createObj 
                   yaxis = [ "gridcolor", box "#222" ] |> JsInterop.createObj 
                   legend = {| orientation = "h" |}
                   plot_bgcolor = "transparent"
                   paper_bgcolor = "transparent" |}

            div [][
                ReactBindings.React.createElement
                    (Ext.plotly,
                     {| data = data
                        layout = layout
                        useResizeHandler = true
                        style = {| marginTop = "-90px" |} |}, [])
            ]
    , memoizeWith = equalsButFunctions)
