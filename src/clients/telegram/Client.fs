module Web.Clients.Telegram.Client

open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Clients.Domain.Telegram

let private clients = ClientFactory()

let init (connection: Connection) =
    let token = connection.Token

    match clients.TryGetValue token with
    | true, client -> Ok client
    | _ ->
        try
            let client = Client(token)
            clients.TryAdd(token, client) |> ignore
            Ok client
        with ex ->
            Error
            <| Operation {
                Message = ex |> Exception.toMessage
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
