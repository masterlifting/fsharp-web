module Web.Http.Client

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Http.Domain

let private clients = ClientFactory()

let private create (baseUrl: Uri) =
    try
        let client = new Client()
        client.BaseAddress <- baseUrl
        Ok client
    with ex ->
        Error
        <| Operation
            { Message = ex |> Exception.toMessage
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }

let init (baseUrl: string) (headers: Headers) =
    baseUrl
    |> Route.toUri
    |> Result.bind (fun uri ->

        match clients.TryGetValue baseUrl with
        | true, client -> Ok client
        | _ ->
            create uri
            |> Result.bind (fun client ->
                client
                |> Headers.set headers
                |> Result.map (fun client ->
                    clients.TryAdd(baseUrl, client) |> ignore
                    client)))
