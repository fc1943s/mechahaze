namespace MechaHaze.Shared

open Expecto
open Expecto.Flip
open MechaHaze.Shared.Bindings

module Tests =
    let tests =
        testList
            "Tests"
            [

                testList
                    "Bindings"
                    [

                        test "Bindings" {
                            let apply (source, dest) =
                                applyBinding (Binding (BindingSourceId source, BindingDestId dest))

                            let expected (bindings: (string * string) list) =
                                bindings
                                |> List.map (fun (source, dest) -> Binding (BindingSourceId source, BindingDestId dest))

                            let preset = Preset.Default

                            let preset = apply ("a", "y") preset

                            preset.Bindings
                            |> Expect.equal "" (expected [ ("a", "y") ])

                            let preset = apply ("a", "y") preset

                            preset.Bindings
                            |> Expect.equal "" (expected [ ("", "y") ])

                            let preset = apply ("a", "y") preset

                            preset.Bindings
                            |> Expect.equal "" (expected [ ("a", "y") ])

                            let preset = apply ("a", "y") preset

                            preset.Bindings
                            |> Expect.equal "" (expected [ ("", "y") ])

                            let preset = apply ("", "y") preset

                            preset.Bindings |> Expect.isEmpty ""
                        }
                    ]
            ]
