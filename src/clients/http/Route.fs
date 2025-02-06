module Web.Http.Route

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Http.Domain.Client

let toUri (url: string) =
    try
        Ok <| Uri url
    with ex ->
        Error <| NotSupported ex.Message

let fromQueryParams (queryParams: string) =
    queryParams.Split '&'
    |> Array.map (fun parameter ->
        match parameter.Split('=') with
        | parts when parts.Length = 2 -> Ok <| (parts[0], parts[1])
        | _ -> Error <| NotSupported $"Query parameter '{parameter}'")
    |> Result.choose
    |> Result.map Map

let toQueryParams (uri: Uri) =
    uri.Query.TrimStart '?' |> fromQueryParams
