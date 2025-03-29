module Web.Clients.Http.Route

open System
open Infrastructure.Domain
open Infrastructure.Prelude

let toUri (url: string) =
    try
        Ok <| Uri url
    with ex ->
        Error
        <| Operation {
            Message = $"Failed to parse url '{url}'. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }

let fromQueryParams (queryParams: string) =
    queryParams.Split '&'
    |> Array.map (fun parameter ->
        match parameter.Split('=') with
        | parts when parts.Length = 2 -> Ok <| (parts[0], parts[1])
        | _ -> Error <| NotSupported $"Http query parameter '{parameter}' is not supported.")
    |> Result.choose
    |> Result.map Map

let toQueryParams (uri: Uri) =
    uri.Query.TrimStart '?' |> fromQueryParams
