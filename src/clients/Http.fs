module Web.Http

open System
open Infrastructure
open Infrastructure.Domain.Errors

type Client = Net.Http.HttpClient

let toUri (url: string) =
    try
        Ok <| Uri url
    with ex ->
        Error <| Parsing ex.Message

let toQueryParams (uri: Uri) =
    let query = uri.Query.TrimStart('?')

    query.Split('&')
    |> Array.map (fun parameter ->
        match parameter.Split('=') with
        | parts when parts.Length = 2 -> Ok(parts.[0], parts.[1])
        | _ -> Error <| Parsing $"Invalid query parameter '{parameter}' in '{uri}'")
    |> Dsl.Seq.roe
    |> Result.map Map

let internal create (baseUrl: string) =
    baseUrl
    |> toUri
    |> Result.mapError Infrastructure
    |> Result.map (fun uri ->
        let client = new Client()
        client.BaseAddress <- uri
        client)

let get (client: Client) (path: string) =
    async {
        try
            let! response = client.GetAsync(path) |> Async.AwaitTask

            match response.IsSuccessStatusCode with
            | true ->
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return Ok content
            | false -> return Error(Infrastructure(InvalidResponse response.ReasonPhrase))

        with ex ->
            return Error(Infrastructure(InvalidRequest ex.Message))
    }

let post (client: Client) (path: string) (data: byte[]) =
    async {
        try
            let! response = client.PostAsync(path, new Net.Http.ByteArrayContent(data)) |> Async.AwaitTask

            match response.IsSuccessStatusCode with
            | true ->
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return Ok content
            | false -> return Error <| InvalidResponse response.ReasonPhrase

        with ex ->
            return Error <| InvalidRequest ex.Message
    }
