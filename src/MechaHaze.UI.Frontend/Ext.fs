namespace MechaHaze.UI.Frontend

open Browser
open Fable.Core
open Fable.Core.JsInterop

module Ext =

    // TODO: put inside function to avoid automatic computations?

    JsInterop.importAll "typeface-roboto-condensed"


    JsInterop.importAll "./node_modules/@fortawesome/fontawesome-free/css/all.css"

    JsInterop.importAll "./node_modules/bulma/bulma.sass"
    JsInterop.importAll "./node_modules/bulma-extensions/dist/css/bulma-extensions.min.css"
    JsInterop.importAll "./node_modules/bulmaswatch/cyborg/bulmaswatch.scss"

    JsInterop.importAll "./public/index.scss"
    JsInterop.importAll "./public/index.ts"
    JsInterop.importAll "./public/index.tsx"
    JsInterop.importAll "./public/index.js"
    JsInterop.importAll "./public/index.jsx"


    let peaks: ExtTypes.IPeaks = importAll "peaks.js"
    let flatted: ExtTypes.IFlatted = importAll "flatted/esm"

    let stormDiagrams: ExtTypes.IStormDiagrams = importAll "@projectstorm/react-diagrams"
    let createStormDiagram: unit -> ExtTypes.IStormDiagramEngine = importDefault "@projectstorm/react-diagrams"
    let stormCanvas: ExtTypes.IStormCanvas = importAll "@projectstorm/react-canvas-core"
    let stormDefaultState: unit -> obj = import "DefaultState" "./public/storm-diagrams/DefaultState.ts"


    let plotly: obj = importDefault "react-plotly.js"

    let moment: obj -> ExtTypes.IMoment = importAll "moment"


    Dom.window?Ext <- {|
                          peaks = peaks
                          flatted = flatted
                          stormDiagrams = stormDiagrams
                          createStormDiagram = createStormDiagram
                          stormCanvas = stormCanvas
                          stormDefaultState = stormDefaultState
                          plotly = plotly
                          moment = moment
                      |}
