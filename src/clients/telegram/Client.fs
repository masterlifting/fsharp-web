module Web.Telegram.Client

open System.Threading
open Infrastructure
open Web.Telegram.Domain

let private clients = ClientFactory()

let private create' (token: string) =
    try
        let client = new Client(token)
        Ok client
    with ex ->
        Error
        <| Operation
            { Message = ex |> Exception.toMessage
              Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }

let create (token: string) =

    match clients.TryGetValue token with
    | true, client -> Ok client
    | _ ->
        create' token
        |> Result.map (fun client ->
            clients.TryAdd(token, client) |> ignore
            client)

let listen (ct: CancellationToken) (client: Client) =
    async { return Error <| NotImplemented "Telegram.listen." }

let sendText (chatId: ChatId) (text: Text) (ct: CancellationToken) =
    async { return Error <| NotImplemented "Telegram.sendText." }

let sendButtonsGroups (chatId: ChatId) (buttonsGroup: ButtonsGroup) (ct: CancellationToken) =
    async { return Error "Telegram.sendButtonGroups not implemented." }
