module Web.Client.Http

open System
open System.Net.Http
open System.Threading
open Infrastructure
open Infrastructure.DSL
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
        | parts when parts.Length = 2 -> Ok(parts[0], parts[1])
        | _ -> Error <| Parsing $"Invalid query parameter '{parameter}' in '{uri}'")
    |> Seq.roe
    |> Result.map Map

let private addHeaders (headers: Headers) (client: Client) =
    match headers with
    | Some headers ->
        headers
        |> Map.iter (fun key value -> client.DefaultRequestHeaders.Add(key, value))
    | None -> ()

let private getHeaders (response: HttpResponseMessage) : Headers =
    try
        response.Headers
        |> Seq.map (fun header -> header.Key, header.Value |> Seq.toArray)
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

module Request =
    module Get =
        let private create' getContent =
            fun (request: Request) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> addHeaders request.Headers
                        let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            let headers = response |> getHeaders
                            return Ok <| (content, headers)
                        | false ->
                            return
                                Error
                                <| Web { Message = response.ReasonPhrase; Code = Some(response.StatusCode |> int) }

                    with ex ->
                        return Error <| Web  { Message = ex.Message; Code = None }
                }

        let private create getContent =
            fun (request: Request) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> addHeaders request.Headers
                        let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            return Ok <| content
                        | false ->
                            return
                                Error
                                <| Web { Message = response.ReasonPhrase; Code = Some(response.StatusCode |> int) }

                    with ex ->
                        return Error <| Web { Message = ex.Message; Code = None }
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
                        client |> addHeaders request.Headers

                        let content =
                            match content with
                            | Bytes data -> new ByteArrayContent(data)
                            | String data -> new StringContent(data.Data, data.Encoding, data.MediaType)

                        let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            let headers = response |> getHeaders
                            return Ok <| (content, headers)
                        | false ->
                            return
                                Error
                                <| Web { Message = response.ReasonPhrase; Code = Some(response.StatusCode |> int) }

                    with ex ->
                        return Error <| Web { Message = ex.Message; Code = None } 
                }

        let private create getContent =
            fun (request: Request) (content: RequestContent) (ct: CancellationToken) (client: Client) ->
                async {
                    try
                        client |> addHeaders request.Headers

                        let content =
                            match content with
                            | Bytes data -> new ByteArrayContent(data)
                            | String data -> new StringContent(data.Data, data.Encoding, data.MediaType)

                        let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                        match response.IsSuccessStatusCode with
                        | true ->
                            let! content = getContent response |> Async.AwaitTask
                            return Ok <| content
                        | false ->
                            return
                                Error
                                <| Web
                                    { Message = response.ReasonPhrase
                                      Code = Some(response.StatusCode |> int) }

                    with ex ->
                        return Error <| Web { Message = ex.Message; Code = None }
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

module Captcha =
    module AntiCaptcha =
        module Domain =
            type Task = { ErrorId: int; TaskId: int }
            type Solution = { Text: string }
            type TaskResult = { Status: string; Solution: Solution }

        open Infrastructure.DSL.AP
        open Infrastructure.DSL.Threading
        open Domain

        let private handleTaskResult ct tryAgain attempts (result: TaskResult) =
            async {
                match result.Status with
                | "processing" ->
                    match ct |> notCanceled with
                    | true ->
                        do! Async.Sleep 500
                        return! tryAgain attempts
                    | _ -> return Error <| Cancelled "AntiCaptcha"
                | "ready" ->
                    return
                        match result.Solution.Text with
                        | IsInt result -> Ok result
                        | _ -> Error <| Parsing $"AntiCaptcha. Is not an integer: '{result.Solution.Text}'."
                | _ ->
                    return
                        Error
                        <| Web
                            { Message = "AntiCaptcha. Not supported status."
                              Code = None }
            }

        let private getTaskResult ct key httpClient task =
            let data =
                $@"
                    {{
                        ""clientKey"":""{key}"",
                        ""taskId"":""{task.TaskId}""
                    }}"

            let request =
                { Path = "/getTaskResult"
                  Headers = None }

            let content =
                String
                    {| Data = data
                       Encoding = Text.Encoding.UTF8
                       MediaType = "application/json" |}

            let rec innerLoop attempts =
                match attempts with
                | 0 -> async { return Error <| Cancelled "AntiCaptcha. No attempts left." }
                | _ ->
                    httpClient
                    |> Request.Post.waitString ct request content
                    |> Response.Json.mapString
                    |> ResultAsync.bind' (handleTaskResult ct innerLoop (attempts - 1))

            innerLoop 10

        let private createTask ct key httpClient image =
            let data =
                $@"
                    {{
                        ""clientKey"": ""%s{key}"",
                        ""task"": {{
                            ""type"": ""ImageToTextTask"",
                            ""body"": ""%s{image |> Convert.ToBase64String}"",
                            ""phrase"": false,
                            ""case"": false,
                            ""numeric"": 0,
                            ""math"": 0,
                            ""minLength"": 0,
                            ""maxLength"": 0
                        }}
                    }}"

            let request = { Path = "/createTask"; Headers = None }

            let content =
                String
                    {| Data = data
                       Encoding = Text.Encoding.UTF8
                       MediaType = "application/json" |}

            httpClient
            |> Request.Post.waitString ct request content
            |> Response.Json.mapString
            |> ResultAsync.bind' (getTaskResult ct key httpClient)

        let solveToInt ct (image: byte array) =
            match image.Length with
            | 0 -> async { return Error <| Configuration "AntiCaptcha. Image is empty." }
            | _ ->
                Configuration.getEnvVar "AntiCaptchaApiKey"
                |> ResultAsync.wrap (fun keyOpt ->
                    match keyOpt with
                    | None -> async { return Error <| Configuration "AntiCaptcha. API Key not found." }
                    | Some key ->

                        let createHttpClient url = create url None

                        let createCaptchaTask =
                            ResultAsync.wrap (fun httpClient -> image |> createTask ct key httpClient)

                        createHttpClient "https://api.anti-captcha.com" |> createCaptchaTask)
