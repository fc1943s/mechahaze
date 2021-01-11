namespace MechaHaze.CoreCLR.Core

module Json =
    let inline deserializeWith<'a> (fn: string -> Result<'a, exn>) (json: string) =
        if typeof<'a> = typeof<string> then
            Ok (unbox<'a> (box json))
        else
            fn json

    module Thoth =
        open Thoth.Json.Net

        let serialize value = Encode.Auto.toString (4, value)

        let inline deserialize<'a> = deserializeWith (Decode.Auto.fromString<'a> >> Result.mapError exn)

    module System =
        open System.Text.Json

        let options =
            let options = JsonSerializerOptions ()
            options.WriteIndented <- true
            options

        let serialize value = JsonSerializer.Serialize (value, options)

        let inline deserialize<'a> =
            deserializeWith (fun json ->
                try
                    Ok (JsonSerializer.Deserialize<'a> (json, options))
                with ex -> Error ex)

    module Net =

        open System.Collections.Generic
        open Newtonsoft.Json
        open Newtonsoft.Json.Serialization
        open Fable.Remoting.Json

        module ContractResolvers =
            let requireObjectProperties =
                { new DefaultContractResolver() with
                    member _.CreateObjectContract objectType =
                        let contract = base.CreateObjectContract objectType
                        //                    contract.ItemRequired <- Nullable Required.Always
                        contract
                }


        let settings =
            JsonSerializerSettings
                (ContractResolver = ContractResolvers.requireObjectProperties,
                 Formatting = Formatting.Indented,
                 Converters =
                     List [
                         FableJsonConverter () :> JsonConverter
                     ])

        let serialize (value: 'a) = JsonConvert.SerializeObject (value, typeof<'a>, settings)

        let inline deserialize<'a> =
            deserializeWith (fun json ->
                try
                    Ok (JsonConvert.DeserializeObject (json, typeof<'a>, settings) :?> 'a)
                with ex -> Error ex)

    let serialize = Thoth.serialize
    let deserialize<'a> = Thoth.deserialize<'a>
