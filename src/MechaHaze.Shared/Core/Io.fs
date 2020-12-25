namespace MechaHaze.Shared.Core

type IoState<'S, 'T> = 'S -> ('T * 'S)


module Io =
    let getState s =
        s, s

    let setState state _ =
        (), state

    let inline run state x =
        x state

    let mapState fn s =
        let mapper state =
            let x, state = run state s
            fn x, state
        mapper


    type StateBuilder () =
        member this.Zero () s =
            (), s
        member this.Return x s =
            x, s
        member inline this.ReturnFrom x =
            x
        member this.Delay fn =
            fn ()
        member this.Bind (x, fn) state =
            let result, state = run state x
            run state (fn result)

        member this.Combine (x1, x2) state =
            let state = run state x1 |> snd
            run state x2

        member this.For (seq, fn) =
            seq
            |> Seq.map fn
            |> Seq.reduceBack (fun x1 x2 -> this.Combine (x1, x2))

        member this.While (fn, x) =
            if fn ()
            then this.Combine (x, this.While (fn, x))
            else this.Zero ()

    let state = StateBuilder ()


