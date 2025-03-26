[<RequireQualifiedAccess>]
module Web.Clients.Http.Response

open System
open System.Text.Json
open System.Threading
open System.Net.Http
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open Web.Clients.Domain.Http.Response

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
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
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
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
        }

    let fromJson<'a> (response: Async<Result<string, Error'>>) =
        let options = JsonSerializerOptions()
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        response |> ResultAsync.bind (Json.deserialize'<'a> options)

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
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
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
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
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
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
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
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
        }

module Unit =
    let read (response: Async<Result<HttpResponseMessage, Error'>>) =
        async {
            try
                match! response with
                | Ok _ -> return Ok()
                | Error error -> return Error error
            with ex ->
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
        }
