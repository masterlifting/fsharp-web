module Web.Core

open System
open Infrastructure

module Http =
    module Mapper =
        let toUri (url: string) =
            try
                Ok <| Uri url
            with ex ->
                Error ex.Message

        let toQueryParams (uri: Uri) =
            uri.Query.Split('&')
            |> Array.map (fun parameter ->
                match parameter.Split('=') with
                | parts when parts.Length = 2 -> Ok(parts.[0], parts.[1])
                | _ -> Error $"Invalid query parameter of '{parameter}'.")
            |> DSL.Seq.resultOrError
            |> Result.map Map

    let get (url: Uri) =
        async { return Error "Http.get not implemented." }

    let post (url: Uri) (data: byte[]) =
        async { return Error "Http.post not implemented." }
