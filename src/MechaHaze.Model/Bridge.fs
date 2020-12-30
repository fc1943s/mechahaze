namespace MechaHaze.Model


[<RequireQualifiedAccess>]
module Bridge =

    module Endpoints =
        let protocol = "http"
        let host = "mechahaze"
        let apiPort = 8085
        let socketPath = "/sync"
