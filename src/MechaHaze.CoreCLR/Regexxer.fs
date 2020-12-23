namespace MechaHaze.CoreCLR

open System.Collections.Generic
open System.Text.RegularExpressions

module Regexxer =
    let defaultOptions = RegexOptions.IgnoreCase ||| RegexOptions.Multiline

    // TODO: tests + idiomatic f# rewrite
    let matchAllEx (text, pattern, replace, options) =
        let options = defaultArg options defaultOptions

        let res = List<IList<string>> ()
        let reg = Regex (pattern, options)
        let mutable _replacedText = ""
        let mutable _replaceOffset = 0

        let mutable _match = reg.Match text

        while _match.Success do
            let curr = List<string> ()
            res.Add curr

            for i = 1 to _match.Groups.Count - 1 do
                let group = _match.Groups.[i]
                if group.Value = "" then
                    curr.Add ""
                else
                    for v in group.Captures do
                        let mutable _block = v.Value
                        match replace with
                        | Some replace ->
                            _block <- replace _block
                            _replacedText <- text.Substring (_replaceOffset, v.Index - _replaceOffset) + _block
                            _replaceOffset <- v.Index + v.Length
                        | None -> ()

                        curr.Add _block

            _match <- _match.NextMatch ()

        _replacedText <- _replacedText + text.Substring _replaceOffset

        {| Result = res; ReplacedText = _replacedText |}


    let matchAll (text, pattern) =
        matchAllEx(text, pattern, None, None).Result.FirstOrDefault (fun _ -> List<string> () :> IList<_>)

    let matchFirst (text, pattern) =
        matchAll(text, pattern).FirstOrDefault (fun _ -> "")

    let hasMatch (text, pattern) =
        matchAll(text, pattern).Count > 0
