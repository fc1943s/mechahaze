namespace MechaHaze.Shared.Core

open System
open System.Collections

module AlphaNum =
    let sort (a: string) (b: string) =
        let isNum (s: string) i =
            let c = s.Chars i
            c >= '0' && c <= '9'

        let chunk (s: string) f t =
            (f < s.Length) && (t < s.Length) && (isNum s f) = (isNum s t)

        let chunkTo str fn =
            let rec loop str f e =
                if chunk str f e
                then loop str f (e + 1)
                else e
            loop str fn fn

        let intOfString (str: string) =
            str
            |> Int32.TryParse
            |> function
                | true, result -> result
                | _ -> 0

        let rec chunkCmp (a: string) ai (b: string) bi =
            let al, bl = a.Length, b.Length
            if ai >= al || bi >= bl then
                compare al bl
            else
                let ae, be = chunkTo a ai, chunkTo b bi
                let sa, sb = a.Substring (ai, (ae - ai)), b.Substring (bi, (be - bi))

                let cmp =
                    if isNum a ai && isNum b bi
                    then compare (intOfString sa) (intOfString sb)
                    else compare sa sb
                if cmp = 0
                then chunkCmp a ae b be
                else cmp

        chunkCmp a 0 b 0


    type AlphaNumComparer () =
        interface IComparer with
            member this.Compare (x, y) = sort (string x) (string y)
