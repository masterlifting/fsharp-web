[<RequireQualifiedAccessAttribute>]
module Web.Client.Http

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open Infrastructure.DSL
open Infrastructure.Domain.Errors
open Web.Domain.Http

module Route =
    let toUri (url: string) =
        try
            Ok <| Uri url
        with ex ->
            Error <| NotSupported ex.Message

    let toQueryParams (uri: Uri) =
        let query = uri.Query.TrimStart '?'

        query.Split '&'
        |> Array.map (fun parameter ->
            match parameter.Split('=') with
            | parts when parts.Length = 2 -> Ok <| (parts[0], parts[1])
            | _ -> Error <| NotSupported $"Query parameter '{parameter}' in '{uri}'")
        |> Seq.roe
        |> Result.map Map

    let toHost (client: Client) = client.BaseAddress.Host

    let toAbsoluteUri (client: Client) = client.BaseAddress.AbsoluteUri

    let toOrigin (client: Client) =
        client.BaseAddress.GetLeftPart(UriPartial.Authority)

module Headers =
    let add (headers: Headers) (client: Client) =
        match headers with
        | Some headers ->
            headers
            |> Map.iter (fun key values ->
                match client.DefaultRequestHeaders.TryGetValues(key) with
                | true, values' ->
                    client.DefaultRequestHeaders.Remove(key) |> ignore
                    client.DefaultRequestHeaders.Add(key, values' |> Seq.append values |> Seq.rev)
                | _ -> client.DefaultRequestHeaders.Add(key, values))
        | None -> ()

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
        | None -> Error <| NotFound "Headers"
        | Some headers ->
            match headers |> Map.tryFind key with
            | None -> Error <| NotFound $"Header '{key}'"
            | Some values ->
                match values with
                | [] -> Error <| NotFound $"Header '{key}' is empty."
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

let create (baseUrl: string) (headers: Headers) =
    baseUrl
    |> Route.toUri
    |> Result.map (fun uri ->
        let client = new Client()
        client.BaseAddress <- uri
        client |> Headers.add headers
        client)

let close (client: Client) = client.Dispose()

module Request =
    module Get =
        let private create' getContent =
            fun (request: Request) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> Headers.add request.Headers
                        let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            let headers = response |> Headers.get
                            return Ok <| (content, headers)
                        | false ->
                            client.Dispose()

                            return
                                Error
                                <| Operation
                                    { Message = response.ReasonPhrase
                                      Code = response.StatusCode |> string |> Some }

                    with ex ->
                        client.Dispose()
                        return Error <| Operation { Message = ex.Message; Code = None }
                }

        let private create getContent =
            fun (request: Request) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> Headers.add request.Headers
                        let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            return Ok <| content
                        | false ->
                            client.Dispose()

                            return
                                Error
                                <| Operation
                                    { Message = response.ReasonPhrase
                                      Code = response.StatusCode |> string |> Some }

                    with ex ->
                        client.Dispose()
                        return Error <| Operation { Message = ex.Message; Code = None }
                }

        /// <summary>
        /// Get request with monadic response of string.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let string ct request client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStringAsync(ct)

            let get = create getContent
            client |> get request ct

        /// <summary>
        /// Get request with monadic response of string and headers.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let string' ct request client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStringAsync(ct)

            let get = create' getContent
            client |> get request ct

        /// <summary>
        /// Get request with monadic response of byte array.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let bytes ct request client =
            let getContent (response: HttpResponseMessage) =
                response.Content.ReadAsByteArrayAsync(ct)

            let get = create getContent
            client |> get request ct

        /// <summary>
        /// Get request with monadic response of byte array and headers.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let bytes' ct request client =
            let getContent (response: HttpResponseMessage) =
                response.Content.ReadAsByteArrayAsync(ct)

            let get = create' getContent
            client |> get request ct

        /// <summary>
        /// Get request with monadic response of stream.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let stream ct request client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStreamAsync()

            let get = create getContent
            client |> get request ct

        /// <summary>
        /// Get request with monadic response of stream and headers.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let stream' ct request client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStreamAsync()

            let get = create' getContent
            client |> get request ct

    module Post =
        let private create' getContent =
            fun (request: Request) (content: RequestContent) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> Headers.add request.Headers

                        let content =
                            match content with
                            | Bytes data -> new ByteArrayContent(data)
                            | String data ->
                                new StringContent(data.Data, data.Encoding, MediaTypeHeaderValue(data.MediaType))

                        let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            let headers = response |> Headers.get
                            return Ok <| (content, headers)
                        | false ->
                            client.Dispose()

                            return
                                Error
                                <| Operation
                                    { Message = response.ReasonPhrase
                                      Code = response.StatusCode |> string |> Some }

                    with ex ->
                        client.Dispose()
                        return Error <| Operation { Message = ex.Message; Code = None }
                }

        let private create getContent =
            fun (request: Request) (content: RequestContent) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> Headers.add request.Headers

                        let content =
                            match content with
                            | Bytes data -> new ByteArrayContent(data)
                            | String data ->
                                new StringContent(data.Data, data.Encoding, MediaTypeHeaderValue(data.MediaType))

                        let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            return Ok <| content
                        | false ->
                            client.Dispose()

                            return
                                Error
                                <| Operation
                                    { Message = response.ReasonPhrase
                                      Code = response.StatusCode |> string |> Some }

                    with ex ->
                        client.Dispose()
                        return Error <| Operation { Message = ex.Message; Code = None }
                }

        /// <summary>
        /// Post request with monadic response of string.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitString ct request content client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStringAsync(ct)

            let post = create getContent
            client |> post request content ct

        let waitString' ct request content client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStringAsync(ct)

            let post = create' getContent
            client |> post request content ct

        /// <summary>
        /// Post request with monadic response of byte array.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitBytes ct request content client =
            let getContent (response: HttpResponseMessage) =
                response.Content.ReadAsByteArrayAsync(ct)

            let post = create getContent
            client |> post request content ct

        /// <summary>
        /// Post request with monadic response of byte array and headers.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitBytes' ct request content client =
            let getContent (response: HttpResponseMessage) =
                response.Content.ReadAsByteArrayAsync(ct)

            let post = create' getContent
            client |> post request content ct

        /// <summary>
        /// Post request with monadic response of stream.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitStream ct request content client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStreamAsync()

            let post = create getContent
            client |> post request content ct

        /// <summary>
        /// Post request with monadic response of stream and headers.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitStream' ct request content client =
            let getContent (response: HttpResponseMessage) = response.Content.ReadAsStreamAsync()

            let post = create' getContent
            client |> post request content ct

        /// <summary>
        /// Post request with monadic response of unit.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitUnit ct request content client =
            let getContent (_: HttpResponseMessage) =
                async { return () } |> Async.StartAsTask

            let post = create getContent
            client |> post request content ct

        /// <summary>
        /// Post request with monadic response of unit and headers.
        /// </summary>
        /// <param name="request"> The request data. </param>
        /// <param name="content"> The request content. </param>
        /// <param name="ct"> The cancellation token. </param>
        /// <param name="client"> The Http client. </param>
        let waitUnit' ct request content client =
            let getContent (_: HttpResponseMessage) =
                async { return () } |> Async.StartAsTask

            let post = create' getContent
            client |> post request content ct

module Response =
    module Json =
        open Infrastructure.DSL.SerDe

        let mapString<'a> (response: Async<Result<string, Error'>>) =
            response |> ResultAsync.bind (Json.deserialize'<'a> Json.WebApi)

        let mapString'<'a> (response: Async<Result<string * Headers, Error'>>) =
            response
            |> ResultAsync.bind (fun (content, _) -> content |> Json.deserialize'<'a> Json.WebApi)
