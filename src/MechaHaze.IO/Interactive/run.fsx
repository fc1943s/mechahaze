#load "dependencies.fsx"

open Expecto
open Expecto.Flip
open MechaHaze.IO


module RabbitQueue =
    let createBus () =
        printfn "createBus"
        ()

open RabbitQueue

let tests =
    testList "tests" [
        testList (nameof RabbitQueue) [
            test (nameof createBus) {
                RabbitQueue.createBus ()
                Expect.equal "" RabbitQueue.a 6
            }
        ]
    ]

runTests { defaultConfig with verbosity = Logging.Debug } tests
