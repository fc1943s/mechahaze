namespace MechaHaze.UI.Frontend

open Fable.Core

module ExtTypes =
    
    type IPeaks =
        abstract init: obj -> obj -> obj

    type IFlatted =
        abstract stringify: obj -> string

    type IMoment =
        abstract diff: IMoment -> string -> bool -> float


    [<AbstractClass>]
    type IStormInteractive =
        abstract registerListener: obj -> unit
        
        member this.AddSelectionChangedEvent (x: obj -> unit) = this.registerListener {| selectionChanged = x |}
        member this.AddEntityRemovedEvent (x: obj -> unit) = this.registerListener {| entityRemoved = (fun a b c -> printfn "B %A %A %A" a b c; x a;) |}


    [<AbstractClass>]
    type IStormLinkModel =
        inherit IStormInteractive
        
        abstract setColor: string -> unit
        
        member this.AddSourcePortChangedEvent (x: obj -> unit) = this.registerListener {| sourcePortChanged = (fun a b c -> printfn "C %A %A %A" a b c; x a;) |}
        member this.AddTargetPortChangedEvent (x: obj -> unit) = this.registerListener {| targetPortChanged = (fun a b c -> printfn "D %A %A %A" a b c; x a;) |}
        
    [<AbstractClass>]
    type IStormPortModel =
        abstract link: IStormPortModel -> IStormLinkModel
            
    [<AbstractClass>]
    type IStormNodeModel =
        inherit IStormInteractive
        
        abstract addOutPort: string -> IStormPortModel
        abstract addInPort: string -> IStormPortModel
        abstract setPosition: int -> int -> unit
            
    [<AbstractClass>]
    type IStormDiagramModel =
        inherit IStormInteractive
        
        abstract addAll: 'a * 'b -> unit
        abstract addAll: 'a * 'b * 'c -> unit
        abstract addLink: IStormLinkModel -> unit
        abstract addNode: IStormNodeModel -> unit
        abstract serializeDiagram: unit -> obj
        abstract deSerializeDiagram: obj -> unit
        
        member this.AddNodesUpdatedEvent (x: obj -> unit) = this.registerListener {| nodesUpdated = (fun a b c -> printfn "E %A %A %A" a b c; x a;) |}
        member this.AddLinksUpdatedEvent (x: obj -> unit) = this.registerListener {| linksUpdated = x |}
        member this.AddOffsetUpdatedEvent (x: obj -> unit) = this.registerListener {| offsetUpdated = (fun a b c -> printfn "G %A %A %A" a b c; x a;) |}
        member this.AddZoomUpdatedEvent (x: obj -> unit) = this.registerListener {| zoomUpdated = (fun a b c -> printfn "H %A %A %A" a b c; x a;) |}
        member this.AddGridUpdatedEvent (x: obj -> unit) = this.registerListener {| gridUpdated = (fun a b c -> printfn "I %A %A %A" a b c; x a;) |}
        
    [<AbstractClass>]
    type IStormStateMachine =
        abstract pushState: obj -> unit
        
    [<AbstractClass>]
    type IStormDiagramEngine =
        abstract setModel: IStormDiagramModel -> unit
        abstract stateMachine: IStormStateMachine
        
        
    type IStormDiagramWidget =
        class end
        
    [<AbstractClass>]
    type IStormDiagrams =
        abstract DiagramModel: obj
        abstract DefaultNodeModel: obj
        
        member this.CreateDiagramModel () = 
            JsInterop.createNew this.DiagramModel () :?> IStormDiagramModel
            
        member this.CreateDefaultNodeModel name color = 
            JsInterop.createNew this.DefaultNodeModel {| name = name; color = color |} :?> IStormNodeModel
            
    [<AbstractClass>]
    type IStormCanvas =
        abstract CanvasWidget: obj
