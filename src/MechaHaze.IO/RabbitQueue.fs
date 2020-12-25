namespace MechaHaze.IO

open RabbitMQ.Client
open System
open EasyNetQ
open Serilog

module RabbitQueue =
    let a = 3

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

    type private BusConnection =
        {
            Address: string
            Port: uint16
            Username: string
            Password: string
        }

    let private buildConnectionString busConnection =
        $"virtualHost=mechahaze;host={busConnection.Address}:{busConnection.Port};"
        + $"username={busConnection.Port};password={busConnection.Password}"

    let createBus virtualHost address username password =
        ()
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




    type Exchange<'T when 'T: not struct> (bus: IBus) =

        let exchange = bus.Advanced.ExchangeDeclare ($"@@Exchange-{typeof<'T>.FullName}", ExchangeType.Topic)

        member _.Post routingKey message =
            try
                bus.Advanced.Publish<'T> (exchange, routingKey, false, Message message)
            with ex -> Log.Error (ex, "Error while publishing message: {A}", message)


        member this.RegisterConsumer bindingKeys (handler: 'T -> Exchange<'T> -> Async<unit>) =
            let queue = bus.Advanced.QueueDeclare ()

            for bindingKey in bindingKeys do
                bus.Advanced.Bind (exchange, queue, bindingKey)
                |> ignore

            async {
                let onMessage (body: IMessage<'T>) (__info: MessageReceivedInfo) = handler body.Body this |> Async.Start

                use _ = bus.Advanced.Consume (queue, onMessage)

                while true do
                    do! Async.Sleep 1000
            }
            |> Async.Start
