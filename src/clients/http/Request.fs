[<RequireQualifiedAccess>]
module Web.Http.Request

open System
open System.Threading
open System.Net.Http
open System.Net.Http.Headers
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Http.Domain

let get (request: Request) (ct: CancellationToken) (client: HttpClient) =
    async {
        try
            match client |> Headers.set request.Headers with
            | Error error -> return Error error
            | Ok client ->
                let! response = client.GetAsync(request.Path, ct) |> Async.AwaitTask

                match response.IsSuccessStatusCode with
                | true -> return Ok response
                | false ->
                    let createError msg =
                        { Message = msg
                          Code = response.StatusCode |> Http |> Some }
                        |> Operation
                        |> Error

                    return
                        match response.ReasonPhrase with
                        | null -> "Unknown reasonPhrase" |> createError
                        | reasonPhrase -> reasonPhrase |> createError
        with ex ->
            return
                Error
                <| Operation
                    { Message = ex |> Exception.toMessage
                      Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
    }

let post (request: Request) (content: RequestContent) (ct: CancellationToken) (client: HttpClient) =
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
                    let createError msg =
                        { Message = msg
                          Code = response.StatusCode |> Http |> Some }
                        |> Operation
                        |> Error

                    return
                        match response.ReasonPhrase with
                        | null -> "Unknown reasonPhrase" |> createError
                        | reasonPhrase -> reasonPhrase |> createError

        with ex ->
            return
                Error
                <| Operation
                    { Message = ex |> Exception.toMessage
                      Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
    }
