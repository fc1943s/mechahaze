namespace MechaHaze.CoreCLR.Core

open System.Threading.Tasks
open Serilog

module SafeQueue =

    type SafeQueueMessage<'T> =
        | Set of 'T
        | Get of AsyncReplyChannel<'T>
        | IsEmpty of AsyncReplyChannel<bool>

    type SafeQueue<'T> (?onSet: 'T option -> 'T -> Task) =
        let agent =
            MailboxProcessor<SafeQueueMessage<'T>>
                .Start(fun inbox ->
                    async {
                        let mutable _state: 'T option = None

                        while true do
                            let! message = inbox.Receive ()

                            match message with
                            | Set item ->
                                let oldState = _state
                                _state <- Some item

                                match onSet with
                                | Some onSet ->
                                    try
                                        do! onSet oldState item |> Async.AwaitTask
                                    with ex -> Log.Error (ex, "Error setting state on SafeQueue")
                                | _ -> ()

                            | Get channel ->
                                match _state with
                                | Some state -> channel.Reply state

                                | None ->
                                    Log.Warning ("Retrieving uninitialized SafeQueue value. Waiting...")

                                    async {
                                        while _state.IsNone do
                                            do! Async.Sleep 100

                                        channel.Reply _state.Value
                                    }
                                    |> Async.Start

                            | IsEmpty channel -> channel.Reply _state.IsNone
                    })

        member _.Enqueue item = agent.Post (Set item)
        member _.Dequeue () = agent.PostAndReply Get
        member _.IsEmpty () = agent.PostAndReply IsEmpty
