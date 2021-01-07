namespace MechaHaze.CoreCLR.Core

open System.Collections.Generic
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Fable.Remoting.Json
open System

module Json =
    module ContractResolvers =
        let requireObjectProperties =
            { new DefaultContractResolver() with
                member _.CreateObjectContract objectType =
                    let contract = base.CreateObjectContract objectType
                    contract.ItemRequired <- Nullable Required.Always
                    contract
            }

    let converter = FableJsonConverter ()

    let settings =
        JsonSerializerSettings
            (ContractResolver = ContractResolvers.requireObjectProperties,
             Formatting = Formatting.Indented,
             Converters =
                 List
                     ([
                         converter :> JsonConverter
                     ]))

    let serialize (value: 'a) = JsonConvert.SerializeObject (value, typeof<'a>, settings)

    let inline deserialize<'a> (json: string) =
        if typeof<'a> = typeof<string> then
            unbox<'a> (box json)
        else
            JsonConvert.DeserializeObject (json, typeof<'a>, settings) :?> 'a
