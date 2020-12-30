namespace MechaHaze.Model

[<RequireQualifiedAccess>]
module Bridge =

    module Endpoints =
        let apiPort = 8085
        let host = "mechahaze"
        let private protocol = "http"
        let apiBaseUrl = $"{protocol}://{host}:{apiPort}"
        let socketPath = "/sync"
