module Web.Client.Http

open System
open System.Net.Http
open System.Threading
open Infrastructure
open Infrastructure.Dsl
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

let private getHeaders (response: HttpResponseMessage) : Headers =
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

let private getRequest getContent =
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
                        <| Web $"Status code: {response.StatusCode}; Reason: {response.ReasonPhrase}"

            with ex ->
                return Error <| Web ex.Message
        }

/// <summary>
/// Get request with monadic response of string.
/// </summary>
/// <param name="request"> The request data. </param>
/// <param name="ct"> The cancellation token. </param>
/// <param name="client"> The Http client. </param>
let get request ct client =
    let getContent (response: HttpResponseMessage) = response.Content.ReadAsStringAsync(ct)

    let get = getRequest getContent
    client |> get request ct

/// <summary>
/// Get request with monadic response of byte array.
/// </summary>
/// <param name="request"> The request data. </param>
/// <param name="ct"> The cancellation token. </param>
/// <param name="client"> The Http client. </param>
let get' request ct client =
    let getContent (response: HttpResponseMessage) =
        response.Content.ReadAsByteArrayAsync(ct)

    let get = getRequest getContent
    client |> get request ct

/// <summary>
/// Get request with monadic response of stream.
/// </summary>
/// <param name="request"> The request data. </param>
/// <param name="ct"> The cancellation token. </param>
/// <param name="client"> The Http client. </param>
let get'' request ct client =
    let getContent (response: HttpResponseMessage) = response.Content.ReadAsStreamAsync()

    let get = getRequest getContent
    client |> get request ct

let private postRequest getContent =
    fun (request: Request) (content: RequestContent) (ct: CancellationToken) (client: Client) ->
        async {
            try
                client |> addHeaders request.Headers

                let content =
                    match content with
                    | Bytes data -> new ByteArrayContent(data)
                    | String data -> new StringContent(data.Content, data.Encoding, data.MediaType)

                let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                match response.IsSuccessStatusCode with
                | true ->
                    let! content = getContent response |> Async.AwaitTask
                    let headers = response |> getHeaders
                    return Ok <| (content, headers)
                | false ->
                    return
                        Error
                        <| Web $"Status code: {response.StatusCode}; Reason: {response.ReasonPhrase}"

            with ex ->
                return Error <| Web ex.Message
        }

/// <summary>
/// Post request with monadic response of string.
/// </summary>
/// <param name="request"> The request data. </param>
/// <param name="content"> The request content. </param>
/// <param name="ct"> The cancellation token. </param>
/// <param name="client"> The Http client. </param>
let post request content ct client =
    let getContent (response: HttpResponseMessage) = response.Content.ReadAsStringAsync(ct)

    let post = postRequest getContent
    client |> post request content ct

/// <summary>
/// Post request with monadic response of byte array.
/// </summary>
/// <param name="request"> The request data. </param>
/// <param name="content"> The request content. </param>
/// <param name="ct"> The cancellation token. </param>
/// <param name="client"> The Http client. </param>
let post' request content ct client =
    let getContent (response: HttpResponseMessage) =
        response.Content.ReadAsByteArrayAsync(ct)

    let post = postRequest getContent
    client |> post request content ct

/// <summary>
/// Post request with monadic response of stream.
/// </summary>
/// <param name="request"> The request data. </param>
/// <param name="content"> The request content. </param>
/// <param name="ct"> The cancellation token. </param>
/// <param name="client"> The Http client. </param>
let post'' request content ct client =
    let getContent (response: HttpResponseMessage) = response.Content.ReadAsStreamAsync()

    let post = postRequest getContent
    client |> post request content ct
    
module Captcha =
    module AntiCaptcha =
        open Infrastructure.Dsl.SerDe
        open Infrastructure.Dsl.ActivePatterns
        let solveInt image ct =
            create "https://api.anti-captcha.com" None
            |> ResultAsync.wrap (fun client ->
                async {
                    match Configuration.getEnvVar "AntiCaptchaApiKey" with
                    | Error error -> return Error error
                    | Ok None -> return Error <| Configuration "No AntiCaptcha key found in environment variables."
                    | Ok(Some key) ->
                        let data =
                            $@"
                                {{
                                    ""clientKey"": ""{key}"",
                                    ""task"": {{
                                        ""type"": ""ImageToTextTask"",
                                        ""body"": ""{image |> Convert.ToBase64String}"",
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
                                {| Content = data
                                   Encoding = Text.Encoding.UTF8
                                   MediaType = "application/json" |}

                        match! client |> post request content ct with
                        | Error error -> return Error error
                        | Ok(content, _) ->
                            match content |> Json.deserialize'<{| ErrorId: int; TaskId: int |}> Json.WebApi with
                            | Error error -> return Error error
                            | Ok task ->
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
                                        {| Content = data
                                           Encoding = Text.Encoding.UTF8
                                           MediaType = "application/json" |}

                                let rec solve attempts =
                                    async {
                                        match attempts with
                                        | 0 -> return Error <| Web "Failed to solve captcha."
                                        | _ ->
                                            match! client |> post request content ct with
                                            | Error error -> return Error error
                                            | Ok(content, _) ->
                                                match
                                                    content
                                                    |> Json.deserialize'<
                                                        {| Status: string
                                                           Solution: {| Text: string |} |}
                                                        >
                                                        Json.WebApi
                                                with
                                                | Error error -> return Error error
                                                | Ok result ->
                                                    match result.Status with
                                                    | "processing" ->
                                                        do! Async.Sleep 500
                                                        return! solve (attempts - 1)
                                                    | "ready" ->
                                                        match result.Solution.Text with
                                                        | IsInt result -> return Ok result
                                                        | _ -> return Error <| Web "Failed to solve captcha."
                                                    | _ -> return Error <| Web "Failed to solve captcha."
                                    }

                                return! solve 10
                })
