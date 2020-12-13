namespace MechaHaze.Shared

open System

module Core =
    let getTimestamp (date: DateTime) = date.ToString "yyyyMMddHHmmssfff"
