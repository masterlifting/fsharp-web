module Web.Http.Client

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open Infrastructure
open Web.Http.Domain

[<RequireQualifiedAccess>]
module Route =
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
        |> Seq.roe
        |> Result.map Map

    let toQueryParams (uri: Uri) =
        uri.Query.TrimStart '?' |> fromQueryParams

    let toHost (client: Client) = client.BaseAddress.Host

    let toAbsoluteUri (client: Client) = client.BaseAddress.AbsoluteUri

    let toOrigin (client: Client) =
        client.BaseAddress.GetLeftPart(UriPartial.Authority)

[<RequireQualifiedAccess>]
module Headers =

    let private update (key: string) (values: string seq) (client: Client) =
        try
            let values = values |> Seq.distinct
            client.DefaultRequestHeaders.Remove key |> ignore
            client.DefaultRequestHeaders.Add(key, values) |> Ok
        with ex ->
            let message = ex |> Exception.toMessage
            Error <| Operation { Message = message; Code = None }

    let set (headers: Headers) (client: Client) =
        match headers with
        | Some headers ->
            headers
            |> Seq.map (fun x ->
                let values =
                    match client.DefaultRequestHeaders.TryGetValues x.Key with
                    | true, existingValues -> x.Value |> Seq.append existingValues
                    | _ -> x.Value
                    |> Seq.toList

                (x.Key, values))
            |> Seq.map (fun (key, values) -> client |> update key values)
            |> Seq.roe
            |> Result.map (fun _ -> client)
        | None -> Ok client

    let get (response: HttpResponseMessage) : Headers =
        try
            response.Headers
            |> Seq.map (fun header -> header.Key, header.Value |> Seq.toList)
            |> Map
            |> Some
        with _ ->
            None

    let find (key: string) (patterns: string seq) (headers: Headers) =
        match headers with
        | None -> Error <| NotFound "Headers."
        | Some headers ->
            match headers |> Map.tryFind key with
            | None -> Error <| NotFound $"Required header '{key}'."
            | Some values ->
                match values with
                | [] -> Error <| NotFound $"Required value of the header '{key}'."
                | items ->
                    items
                    |> Seq.map (fun x ->
                        match x.Split ';' with
                        | [||] ->
                            match patterns |> Seq.exists x.Contains with
                            | true -> Some [ x ]
                            | _ -> None
                        | parts ->
                            parts
                            |> Array.filter (fun x -> patterns |> Seq.exists x.Contains)
                            |> Array.toList
                            |> Some)
                    |> Seq.choose id
                    |> Seq.concat
                    |> Seq.toList
                    |> Ok

    let tryFind (key: string) (patterns: string seq) (headers: Headers) =
        match find key patterns headers with
        | Ok values -> Some values
        | _ -> None

let private clients = ClientFactory()

let create (baseUrl: string) (headers: Headers) =
    baseUrl
    |> Route.toUri
    |> Result.bind (fun uri ->

        match clients.TryGetValue baseUrl with
        | true, client -> Ok client
        | _ ->
            let client = new Client()
            client.BaseAddress <- uri

            client
            |> Headers.set headers
            |> Result.map (fun client ->
                clients.TryAdd(baseUrl, client) |> ignore
                client))

[<RequireQualifiedAccess>]
module Request =

    let get (ct: CancellationToken) (request: Request) (client: Client) =
        async {
            try
                match client |> Headers.set request.Headers with
                | Error error -> return Error error
                | Ok client ->
                    let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                    match response.IsSuccessStatusCode with
                    | true -> return Ok response
                    | false ->
                        return
                            Error
                            <| Operation
                                { Message = response.ReasonPhrase
                                  Code = response.StatusCode |> string |> Some }
            with ex ->
                let message = ex |> Exception.toMessage
                return Error <| Operation { Message = message; Code = None }
        }

    let post (ct: CancellationToken) (request: Request) (content: RequestContent) (client: Client) =
        async {
            try
                match client |> Headers.set request.Headers with
                | Error error -> return Error error
                | Ok client ->

                    let content =
                        match content with
                        | Bytes data -> new ByteArrayContent(data)
                        | String data ->
                            new StringContent(data.Data, data.Encoding, MediaTypeHeaderValue(data.MediaType))

                    let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                    match response.IsSuccessStatusCode with
                    | true -> return Ok response
                    | false ->
                        return
                            Error
                            <| Operation
                                { Message = response.ReasonPhrase
                                  Code = response.StatusCode |> string |> Some }

            with ex ->
                let message = ex |> Exception.toMessage
                return Error <| Operation { Message = message; Code = None }
        }

[<RequireQualifiedAccess>]
module Response =

    module String =
        let read (ct: CancellationToken) (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok response ->
                        let! result = response.Content.ReadAsStringAsync ct |> Async.AwaitTask

                        return
                            Ok
                            <| { StatusCode = response.StatusCode |> int
                                 Headers = response |> Headers.get
                                 Content = result }
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }

        let readContent (ct: CancellationToken) (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok response ->
                        let! result = response.Content.ReadAsStringAsync ct |> Async.AwaitTask

                        return Ok result
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }

        let fromJson<'a> (response: Async<Result<string, Error'>>) =
            response |> ResultAsync.bind (Json.deserialize'<'a> Json.WebApi)

    module Bytes =
        let read (ct: CancellationToken) (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok response ->
                        let! result = response.Content.ReadAsByteArrayAsync ct |> Async.AwaitTask

                        return
                            Ok
                            <| { StatusCode = response.StatusCode |> int
                                 Headers = response |> Headers.get
                                 Content = result }
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }

        let readContent (ct: CancellationToken) (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok response ->
                        let! result = response.Content.ReadAsByteArrayAsync ct |> Async.AwaitTask

                        return Ok result
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }

    module Stream =
        let read (ct: CancellationToken) (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok response ->
                        let! result = response.Content.ReadAsStreamAsync ct |> Async.AwaitTask

                        return
                            Ok
                            <| { StatusCode = response.StatusCode |> int
                                 Headers = response |> Headers.get
                                 Content = result }
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }

        let readContent (ct: CancellationToken) (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok response ->
                        let! result = response.Content.ReadAsStreamAsync ct |> Async.AwaitTask

                        return Ok result
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }

    module Unit =
        let read (response: Async<Result<HttpResponseMessage, Error'>>) =
            async {
                try
                    match! response with
                    | Ok _ -> return Ok()
                    | Error error -> return Error error
                with ex ->
                    let message = ex |> Exception.toMessage
                    return Error <| Operation { Message = message; Code = None }
            }
