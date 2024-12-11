module Web.Http.Client

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Http.Domain

let private clients = ClientFactory()

let private create (baseUrl: Uri) =
    try
        let client = new HttpClient()
        client.BaseAddress <- baseUrl
        Ok client
    with ex ->
        Error
        <| Operation
            { Message = ex |> Exception.toMessage
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }

let init connection =
    connection.BaseUrl
    |> Route.toUri
    |> Result.bind (fun uri ->

        match clients.TryGetValue connection.BaseUrl with
        | true, client -> Ok client
        | _ ->
            create uri
            |> Result.bind (fun client ->
                client
                |> Headers.set connection.Headers
                |> Result.map (fun client ->
                    clients.TryAdd(connection.BaseUrl, client) |> ignore
                    client)))
