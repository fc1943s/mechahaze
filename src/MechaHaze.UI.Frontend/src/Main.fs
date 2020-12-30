namespace MechaHaze.UI.Frontend

open Feliz
open Feliz.Recoil
open MechaHaze.UI.Frontend
open MechaHaze.UI.Frontend.Components


module Main =

    //    importAll "typeface-roboto-condensed"
//
//    importAll "../public/index.tsx"
//    importAll "../public/index.ts"
//    importAll "../public/index.jsx"
//    importAll "../public/index.js"
//
//    React.render (document.getElementById "root") (React.strictMode [ App.App () ])

    let render =
        React.functionComponent (fun () ->
            Html.div [
                Recoil.root [
                    HomePageComponent.``default`` ()

                    Bridge.bridge ()
                ]
            ])

    ReactDOM.render (render, Browser.Dom.document.getElementById "root")
