namespace MechaHaze.IO

open System.Threading.Tasks
open MechaHaze.CoreCLR
open MechaHaze.CoreCLR.Core
open MechaHaze.Shared.Core
open RabbitMQ.Client
open System
open EasyNetQ
open RabbitMQ.Client.Exceptions
open Serilog
open FSharp.Control.Tasks

module RabbitQueue =

    type private NamingConventions =
        {
            RetrieveQueueName: Type -> string -> string
            RetrieveExchangeName: Type -> string
            RetrieveErrorQueueName: MessageReceivedInfo -> string
            RetrieveErrorExchangeName: MessageReceivedInfo -> string
        }
        static member inline Default =
            {
                RetrieveQueueName =
                    fun messageType subscriptionId ->
                        "@@Queue: "
                        + (messageType.FullName + subscriptionId)
                RetrieveExchangeName = fun messageType -> "@@Exchange: " + messageType.FullName
                RetrieveErrorQueueName = fun messageReceivedInfo -> messageReceivedInfo.Queue + " - @@Error"
                RetrieveErrorExchangeName = fun messageReceivedInfo -> messageReceivedInfo.Exchange + " - @@Error"
            }

    let createBus virtualHost address username password =
        let connectionString =
            $"virtualHost={virtualHost};host={address}:{5672};"
            + $"username={username};password={password}"

        let conventions typeNameSerializer =
            Conventions
                (typeNameSerializer,
                 QueueNamingConvention = QueueNameConvention NamingConventions.Default.RetrieveQueueName,
                 ExchangeNamingConvention = ExchangeNameConvention NamingConventions.Default.RetrieveExchangeName,
                 ErrorQueueNamingConvention = ErrorQueueNameConvention NamingConventions.Default.RetrieveErrorQueueName,
                 ErrorExchangeNamingConvention =
                     ErrorExchangeNameConvention NamingConventions.Default.RetrieveErrorExchangeName) :> IConventions

        let registerServices =
            fun (services: DI.IServiceRegister) ->
                services.Register<IConventions> (fun (c: DI.IServiceResolver) ->
                    conventions (c.Resolve<ITypeNameSerializer> ()))
                |> ignore

        RabbitHutch.CreateBus (connectionString, registerServices)


    let registerUser (bus: IBus) =
        task {
            try
                let ctl =
                    (SharedConfig.pathsMemoizedLazy ())
                        .rabbitMQ.rabbitMQCtl

                let! _ = Runtime.startProcessAsync ctl "add_user root root"
                let! _ = Runtime.startProcessAsync ctl "set_user_tags root administrator"
                let! _ = Runtime.startProcessAsync ctl "add_vhost mechahaze"
                let! _ = Runtime.startProcessAsync ctl "set_permissions --vhost mechahaze root \".*\" \".*\" \".*\""

                return Ok ()
            with ex -> return Error ex
        }

    let rec declareExchange<'T> (bus: IBus) =
        task {
            try
                let! exchange =
                    bus.Advanced.ExchangeDeclareAsync ($"@@Exchange-{typeof<'T>.FullName}", ExchangeType.Topic)

                return Ok exchange
            with ex ->
                Log.Error (ex, $"Error declaring exchange for {typeof<'T>.FullName}")

                match ex with
                | :? BrokerUnreachableException when ex.InnerException.Message.Contains "ACCESS_REFUSED" ->
                    match! registerUser bus with
                    | Ok () -> return! declareExchange bus
                    | Error ex -> return Error ex
                | _ -> return Error ex
        }


    type Exchange<'T when 'T: not struct> (bus: IBus) =
        let exchange =
            (declareExchange<'T> bus).GetAwaiter().GetResult()
            |> Result.unwrap

        member _.PostAsync routingKey message =
            try
                bus.Advanced.PublishAsync<'T> (exchange, routingKey, false, Message message)
            with ex ->
                Log.Error (ex, "Error while publishing message: {A}", message)
                Task.CompletedTask


        member this.RegisterConsumer routingKeys (handler: 'T -> Exchange<'T> -> Task) cancellationToken =
            task {
                let! queue = bus.Advanced.QueueDeclareAsync ()

                let! _ =
                    routingKeys
                    |> Seq.map (fun routingKey ->
                        bus.Advanced.BindAsync (exchange, queue, routingKey, cancellationToken))
                    |> Task.WhenAll

                let onMessage (body: IMessage<'T>) (__info: MessageReceivedInfo) = handler body.Body this

                use _ = bus.Advanced.Consume (queue, onMessage)

                while true do
                    do! Task.Delay 1000
            }
