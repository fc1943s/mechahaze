namespace MechaHaze.Shared


module Bindings =
    let separator = '|'

    let sources =
        {|
            Levels = "levels"
            Pitch = "pitch"
            PanL = "panl"
            PanR = "panr"
        |}

    let destinations = {| Resolume = "resolume"; Magic = "magic" |}

    let layers =
        [
            "all", ""
            "bass", "bass"
            "content", "other"
            "drums", "drums"
            "kick", "drums"
            "kick-bass", "drums"
            "synths-fx", "other"
            "ups-atm", "vocals"
            "vocals", "vocals"
        ]


    type PresetId = PresetId of presetId: string
    type BindingSourceId = BindingSourceId of bindingSourceId: string
    type BindingDestId = BindingDestId of bindingDestId: string
    type Binding = Binding of source: BindingSourceId * dest: BindingDestId
    let ofBinding (Binding (source, dest)) = source, dest

    type BindingToggle = BindingToggle of presetId: PresetId * Binding

    type Preset =
        {
            Bindings: Binding list
        }
        static member inline Default = { Bindings = [] }


    type PresetMap = PresetMap of Map<PresetId, Preset>
    let ofPresetMap (PresetMap x) = x


    let splitPreset (preset: Preset) =
        let map (str: string) =
            match str.Split separator |> Array.toList with
            | _ :: b :: _ -> str, b
            | _ -> "", ""

        preset.Bindings
        |> List.map (fun (Binding (BindingSourceId sourceId, BindingDestId destId)) -> map sourceId, map destId)

    let applyBinding (Binding (BindingSourceId sourceId, dest) as binding) (preset: Preset) =
        let newBindings =
            preset.Bindings
            |> List.tryFind (fun binding -> binding |> ofBinding |> snd |> (=) dest)
            |> function
            | None -> binding :: preset.Bindings
            | Some (Binding (BindingSourceId oldSourceId, _)) ->
                preset.Bindings
                |> List.filter (fun binding -> binding |> ofBinding |> snd |> (<>) dest)
                |> List.append [
                    match oldSourceId, sourceId with
                    | "", "" -> ()
                    | "", _ -> binding
                    | _ -> Binding (BindingSourceId "", dest)
                   ]

        { preset with Bindings = newBindings }
