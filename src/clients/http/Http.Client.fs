module Web.Client.Http

open System
open System.Threading
open Infrastructure
open Infrastructure.Domain.Errors
open Web.Domain.Http

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

let private addHeaders (headers: Headers) (client: Client) =
    match headers with
    | Some headers ->
        headers
        |> Map.iter (fun key value -> client.DefaultRequestHeaders.Add(key, value))
    | None -> ()

let private getHeaders (response: Net.Http.HttpResponseMessage) =
    try
        response.Headers
        |> Seq.map (fun header -> header.Key, header.Value |> Seq.head)
        |> Map
        |> Some
    with _ ->
        None


let create (baseUrl: string) (headers: Headers) =
    baseUrl
    |> toUri
    |> Result.map (fun uri ->
        let client = new Client()
        client.BaseAddress <- uri
        client |> addHeaders headers
        client)

let getString (path: string) (headers: Headers) (ct: CancellationToken) (client: Client) =
    async {
        try
            client |> addHeaders headers
            let! response = client.GetAsync(path, ct) |> Async.AwaitTask

            match response.IsSuccessStatusCode with
            | true ->
                let! content = response.Content.ReadAsStringAsync(ct) |> Async.AwaitTask
                let headers = response |> getHeaders
                return Ok <| (content, headers)
            | false -> return Error <| Web response.ReasonPhrase

        with ex ->
            return Error <| Web ex.Message
    }

let getBytes (path: string) (headers: Headers) (ct: CancellationToken) (client: Client) =
    async {
        try
            client |> addHeaders headers
            let! response = client.GetAsync(path, ct) |> Async.AwaitTask

            match response.IsSuccessStatusCode with
            | true ->
                let! content = response.Content.ReadAsByteArrayAsync(ct) |> Async.AwaitTask
                let headers = response |> getHeaders
                return Ok <| (content, headers)
            | false -> return Error <| Web response.ReasonPhrase

        with ex ->
            return Error <| Web ex.Message
    }

let post (path: string) (data: byte[]) (headers: Headers) (ct: CancellationToken) (client: Client) =
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
            | false -> return Error <| Web response.ReasonPhrase

        with ex ->
            return Error <| Web ex.Message
    }
