namespace MechaHaze.Shared

open Suigetsu.Core

module Bindings =
    let separator = '|'

    let sources =
        {| Levels = "levels"
           Pitch = "pitch"
           PanL = "panl"
           PanR = "panr" |}

    let destinations =
        {| Resolume = "resolume"
           Magic = "magic" |}
           
    let layers =
        [ "all", ""
          "bass", "bass"
          "content", "other"
          "drums", "drums"
          "kick", "drums"
          "kick-bass", "drums"
          "synths-fx", "other"
          "ups-atm", "vocals"
          "vocals", "vocals" ]
           

    type Binding = Binding of source:string * dest:string
    let ofBinding (Binding (source, dest)) = source, dest

    type BindingToggle = BindingToggle of presetName:string * Binding

    type Preset = Preset of Binding list
    let ofPreset (Preset x) = x

    type PresetMap = PresetMap of Map<string, Preset>
    let ofPresetMap (PresetMap x) = x


    let splitPreset (Preset preset) =
        preset
        |> List.map (
            ofBinding
            >> Tuple2.map (fun id ->
                match id.Split separator |> Array.toList with
                | _ :: b :: _ -> id, b
                | _ -> "", ""
            )
        )
        
    let applyBinding (Binding (source, dest) as binding) (Preset preset) =
        preset
        |> List.tryFind (ofBinding >> snd >> (=) dest)
        |> function
            | None -> binding :: preset
            | Some (Binding (oldSource, _)) ->
                preset
                |> List.filter (ofBinding >> snd >> (<>) dest)
                |> List.append
                       [ match oldSource, source with
                         | "", "" -> ()
                         | "", _ -> binding
                         | _ -> Binding ("", dest) ]
        |> Preset
