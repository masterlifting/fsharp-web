module Web.Http

open System
open System.Threading
open Infrastructure
open Infrastructure.Domain.Errors

type Client = Net.Http.HttpClient

module Domain =
    type Headers = Map<string, string> option

    type Request =
        | Get of string * Headers
        | Post of string * byte[] * Headers

    type Response = string * Headers

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

open Domain

let get (client: Client) (path: string) (headers: Headers) (ct: CancellationToken) =
    async {
        try
            let! response = client.GetAsync(path, ct) |> Async.AwaitTask

            match response.IsSuccessStatusCode with
            | true ->
                let! content = response.Content.ReadAsStringAsync(ct) |> Async.AwaitTask

                let headers =
                    response.Headers
                    |> Option.ofObj
                    |> Option.map (fun headers -> Map<string, string> [])

                return Ok <| Response(content, headers)
            | false -> return Error(Infrastructure(InvalidResponse response.ReasonPhrase))

        with ex ->
            return Error(Infrastructure(InvalidRequest ex.Message))
    }

let post (client: Client) (path: string) (data: byte[]) (headers: Headers) (ct: CancellationToken) =
    async {
        try
            let! response =
                client.PostAsync(path, new Net.Http.ByteArrayContent(data), ct)
                |> Async.AwaitTask

            match response.IsSuccessStatusCode with
            | true ->
                let! content = response.Content.ReadAsStringAsync(ct) |> Async.AwaitTask

                let headers =
                    response.Headers
                    |> Option.ofObj
                    |> Option.map (fun headers -> Map<string, string> [])

                return Ok <| Response(content, headers)
            | false -> return Error(Infrastructure(InvalidResponse response.ReasonPhrase))

        with ex ->
            return Error(Infrastructure(InvalidRequest ex.Message))
    }
