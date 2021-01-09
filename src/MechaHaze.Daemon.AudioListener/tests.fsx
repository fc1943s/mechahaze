#load "dependencies.fsx"

open Expecto
open Expecto.Flip

open MechaHaze.Daemon.AudioListener

module StatePersistence =
    let read () =
        printfn "\n\n----\nRead"
        ()

open StatePersistence

let tests =
    testList "tests" [
        testList (nameof StatePersistence) [
            test (nameof read) {
//                LocalQueue.Timestamp
                Expect.equal "" 9 9
            }
        ]
    ]

runTests { defaultConfig with verbosity = Logging.Debug } tests
