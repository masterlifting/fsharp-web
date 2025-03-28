[<RequireQualifiedAccess>]
module Web.Clients.Http.Request

open System
open System.Threading
open System.Net.Http
open System.Net.Http.Headers
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Http

let get (request: Request) (ct: CancellationToken) (client: Client) =
    async {
        try
            match client |> Headers.set request.Headers with
            | Error error -> return Error error
            | Ok client ->
                let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                match response.IsSuccessStatusCode with
                | true -> return Ok response
                | false ->
                    let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                    let createError reason =
                        Error
                        <| Operation {
                            Message =
                                $"'{client.BaseAddress}{request.Path}' has received the error: '%s{responseContent}'. Reason: '%s{reason}'"
                            Code = response.StatusCode |> Http |> Some
                        }

                    return
                        match response.ReasonPhrase with
                        | null -> "Unknown 'response.ReasonPhrase'" |> createError
                        | reasonPhrase -> reasonPhrase |> createError
        with ex ->
            return
                Error
                <| Operation {
                    Message = $"'{client.BaseAddress}{request.Path}' {ex |> Exception.toMessage}"
                    Code = None
                }
    }

let post (request: Request) (content: RequestContent) (ct: CancellationToken) (client: Client) =
    async {
        try
            match client |> Headers.set request.Headers with
            | Error error -> return Error error
            | Ok client ->

                let content =
                    match content with
                    | Bytes data -> new ByteArrayContent(data)
                    | String data -> new StringContent(data.Data, data.Encoding, MediaTypeHeaderValue(data.MediaType))

                let! response = client.PostAsync(request.Path, content, ct) |> Async.AwaitTask

                match response.IsSuccessStatusCode with
                | true -> return Ok response
                | false ->
                    let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                    let createError reason =
                        Error
                        <| Operation {
                            Message =
                                $"'{client.BaseAddress}{request.Path}' has received the error: '%s{responseContent}'. Reason: '%s{reason}'"
                            Code = response.StatusCode |> Http |> Some
                        }

                    return
                        match response.ReasonPhrase with
                        | null -> "Unknown 'response.ReasonPhrase'" |> createError
                        | reasonPhrase -> reasonPhrase |> createError

        with ex ->
            return
                Error
                <| Operation {
                    Message = $"'{client.BaseAddress}{request.Path}' {ex |> Exception.toMessage}"
                    Code = None
                }
    }
