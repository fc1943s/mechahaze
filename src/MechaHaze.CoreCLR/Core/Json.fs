namespace MechaHaze.CoreCLR.Core

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System

module Json =
    let contractResolvers =
        {| RequireObjectProperties =
               { new DefaultContractResolver () with
                     member _.CreateObjectContract objectType =
                         let contract = base.CreateObjectContract objectType
                         contract.ItemRequired <- Nullable Required.Always
                         contract } |}
