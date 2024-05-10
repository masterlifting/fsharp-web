module Web.Core

open System
open Infrastructure

module Http =
    let get (url: string) =
        async { return Error "Http.get not implemented." }

    let post (url: string) (data: byte[]) =
        async { return Error "Http.post not implemented." }

    let getUri (url: string) =
        try
            Ok <| Uri url
        with ex ->
            Error ex.Message

    let getQueryParameters (uri: Uri) =
        uri.Query.Split('&')
        |> Array.map (fun parameter ->
            match parameter.Split('=') with
            | parts when parts.Length = 2 -> Ok(parts.[0], parts.[1])
            | _ -> Error "Invalid query parameter.")
        |> DSL.Seq.resultOrError
        |> Result.map Map
