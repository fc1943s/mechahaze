namespace MechaHaze.Model

[<RequireQualifiedAccess>]
module Bridge =
    [<RequireQualifiedAccess>]
    type Response =
        | Howdy
        | NewCount of int
        | RandomCharacter of string

    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int
        | RandomCharacter
        | SayHello


    module Endpoints =
        let apiPort = 8085
        let host = "mechahaze"
        let protocol = "https"
        let apiBaseUrl = $"{protocol}://{host}:{apiPort}"
        let socketPath = "/sync"
