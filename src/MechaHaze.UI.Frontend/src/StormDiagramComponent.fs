namespace MechaHaze.UI.Frontend

open System
open Feliz.Recoil
open MechaHaze.Shared.Core
open MechaHaze.UI.Frontend
open MechaHaze.Shared
open Fable.Core
open Fable.React


module StormDiagramComponent =

    type Props =
        {
            BindingsPresetId: Bindings.PresetId
            OnLink: obj -> unit
        }

    type State = { x: unit }

    let randomHsl () =
        "hsla("
        + string (Random().Next 360)
        + ", 85%, 25%, 1)"

    let private createEngine props =
        let preset = Recoil.useValue (AtomFamilies.preset props.BindingsPresetId)
        let bindings = Bindings.splitPreset preset

        match bindings with
        | [] -> None
        | _ :: _ ->
            let engine = Ext.createStormDiagram ()

            let model = Ext.stormDiagrams.CreateDiagramModel ()

            let sources =
                [
                    Bindings.sources.Levels, "#222"
                    Bindings.sources.Pitch, "#444"
                ]
                |> List.mapi (fun i (name, color) ->
                    let node = Ext.stormDiagrams.CreateDefaultNodeModel name color

                    node.setPosition
                        10
                        (10
                         + (i * 40)
                         + (i * (Bindings.layers.Length * 17)))

                    let ports =
                        Bindings.layers
                        |> List.map (fun (layer, warm) ->
                            let port =
                                node.addOutPort
                                    (layer
                                     + (if warm = "" then
                                         ""
                                        else
                                            ($"\u00A0\u00A0->\u00A0\u00A0({warm})")))

                            name + string Bindings.separator + layer, port)

                    model.addNode node
                    node, ports)

            let featureStore =
                [
                    Bindings.destinations.Resolume, "#224"
                    Bindings.destinations.Magic, "#242"
                ]
                |> List.map (fun (name, color) ->
                    let features =
                        bindings
                        |> Seq.filter (fun (_, (fullFeature, _)) ->
                            fullFeature.StartsWith (name + string Bindings.separator))
                        |> Seq.map snd
                        |> Seq.distinct
                        |> Seq.sortWith (fun (_, a) (_, b) -> AlphaNum.sort a b)
                        |> Seq.toList

                    let node = Ext.stormDiagrams.CreateDefaultNodeModel name color

                    features, node)

            let maxFeatureLength =
                bindings
                |> Seq.map (fun (_, (_, feature)) -> feature.Length)
                |> Seq.max
                |> float

            let featureStore =
                featureStore
                |> List.mapi (fun i (features, node) ->
                    let x = 775. - (maxFeatureLength * 4.7) |> int

                    let previousFeatureLength =
                        if i = 0 then
                            0
                        else
                            featureStore
                            |> List.item (i - 1)
                            |> fst
                            |> List.length

                    let y = (10 + (i * 40) + (i * (previousFeatureLength * 17)))

                    node.setPosition x y

                    let ports =
                        features
                        |> List.map (fun (fullFeature, feature) ->
                            let port = node.addInPort feature
                            fullFeature, port)

                    model.addNode node
                    node, ports)

            let sourcesPorts, featurePorts =
                (sources, featureStore)
                |> Tuple2.map (List.map snd)

            let ports =
                sourcesPorts @ featurePorts
                |> List.collect id
                |> Map.ofList

            bindings
            |> List.iter (fun ((fullSource, _), (fullFeature, _)) ->
                (fullSource, fullFeature)
                |> Tuple2.map (fun x -> ports |> Map.tryFind x)
                |> function
                | Some sourcePort, Some destPort ->
                    let link = sourcePort.link destPort

                    link.setColor (randomHsl ())

                    //  link.addSelectionChangedEvent (fun x -> printfn "link SELECTIONCHANGED %A" (Ext.flatted.stringify x))
                    //  link.addSourcePortChangedEvent (fun x -> printfn "SPC")
                    //  link.addTargetPortChangedEvent (fun x -> printfn "TPC")

                    model.addLink link
                | _ -> ())

            //  model.addNodesUpdatedEvent (fun x -> ())
            //  model.addLinksUpdatedEvent (fun x -> printfn "Links updated: %A" (Ext.flatted.stringify x))
            //  model.addOffsetUpdatedEvent (fun x -> ())
            //  model.addZoomUpdatedEvent (fun x -> ())
            //  model.addGridUpdatedEvent (fun x -> ())
            //  model.addSelectionChangedEvent (fun x -> ())
            //  model.addEntityRemovedEvent (fun x -> ())

            engine.setModel model

            let state = JsInterop.createNew Ext.stormDefaultState props.OnLink
            engine.stateMachine.pushState state

            Some engine


    let ``default`` =
        FunctionComponent.Of
            ((fun props ->
                match createEngine props with
                | Some engine ->
                    ReactBindings.React.createElement
                        (Ext.stormCanvas.CanvasWidget,
                         {|
                             engine = engine
                             allowCanvasZoom = false
                             allowCanvasTranslation = false
                         |},
                         [])
                | None -> nothing),
             memoizeWith = equalsButFunctions)
