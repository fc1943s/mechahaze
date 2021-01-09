#load "dependencies.fsx"

open Expecto
open Expecto.Flip

open MechaHaze.IO

open RabbitMQ.Client
open EasyNetQ


module RabbitQueue =
    let createBus () =
        printfn "\n\n----\ncreateBus"
        ()

open RabbitQueue

let tests =
    testList "tests" [
        testList (nameof RabbitQueue) [
            test (nameof createBus) {
                RabbitQueue.createBus ()
                Expect.equal "" 9 9
            }
        ]
    ]

runTests { defaultConfig with verbosity = Logging.Debug } tests
