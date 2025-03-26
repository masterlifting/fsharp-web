module Web.Clients.Http.Client

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Http

let private clients = Http.ClientFactory()

let private create (baseUrl: Uri) =
    try
        let client = new Http.Client()
        client.BaseAddress <- baseUrl
        Ok client
    with ex ->
        Error
        <| Operation
            { Message = ex |> Exception.toMessage
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }

let init (connection: Http.Connection) =
    connection.Host
    |> Route.toUri
    |> Result.bind (fun uri ->

        match clients.TryGetValue connection.Host with
        | true, client -> Ok client
        | _ ->
            create uri
            |> Result.bind (fun client ->
                client
                |> Headers.set connection.Headers
                |> Result.map (fun client ->
                    clients.TryAdd(connection.Host, client) |> ignore
                    client)))
