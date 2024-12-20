﻿module Web.Http.Headers

open System.Net.Http
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Http.Domain

let private update (key: string) (values: string seq) (client: HttpClient) =
    try
        let values = values |> Seq.distinct
        client.DefaultRequestHeaders.Remove key |> ignore
        client.DefaultRequestHeaders.Add(key, values) |> Ok
    with ex ->
        Error
        <| Operation
            { Message = ex |> Exception.toMessage
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }

let set (headers: Headers) (client: HttpClient) =
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
        |> Result.choose
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