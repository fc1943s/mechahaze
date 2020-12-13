namespace MechaHaze.Shared

open Expecto
open Expecto.Flip

module Tests =
    let tests =
        testList
            "Tests"
            [

                testList
                    "Bindings"
                    [

                        test "Bindings" {
                            let apply binding = Bindings.applyBinding (Bindings.Binding binding)

                            let expected = List.map Bindings.Binding >> Bindings.Preset

                            let bindingList = Bindings.Preset []

                            let bindingList = apply ("a", "y") bindingList

                            bindingList
                            |> Expect.equal
                                ""
                                   ([
                                       ("a", "y")
                                    ]
                                    |> expected)

                            let bindingList = apply ("a", "y") bindingList

                            bindingList
                            |> Expect.equal
                                ""
                                   ([
                                       ("", "y")
                                    ]
                                    |> expected)

                            let bindingList = apply ("a", "y") bindingList

                            bindingList
                            |> Expect.equal
                                ""
                                   ([
                                       ("a", "y")
                                    ]
                                    |> expected)

                            let bindingList = apply ("a", "y") bindingList

                            bindingList
                            |> Expect.equal
                                ""
                                   ([
                                       ("", "y")
                                    ]
                                    |> expected)

                            let bindingList = apply ("", "y") bindingList

                            bindingList
                            |> Bindings.ofPreset
                            |> Expect.isEmpty ""
                        }
                    ]
            ]
