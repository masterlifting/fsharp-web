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

let private createByToken token =
    match clients.TryGetValue token with
    | true, client -> Ok client
    | _ ->
        create' token
        |> Result.map (fun client ->
            clients.TryAdd(token, client) |> ignore
            client)

let private createByTokenEnvVar key =
    Configuration.getEnvVar key
    |> Result.bind (
        Option.map Ok
        >> Option.defaultValue (Error <| NotFound $"Environment variable '{key}'.")
    )
    |> Result.bind createByToken

let create way =
    match way with
    | Token token -> createByToken token
    | TokenEnvVar key -> createByTokenEnvVar key

let listen (ct: CancellationToken) (client: Client) =
    async { return Error <| NotImplemented "Telegram.listen." }

let sendText (chatId: ChatId) (text: Text) (ct: CancellationToken) =
    async { return Error <| NotImplemented "Telegram.sendText." }

let sendButtonsGroups (chatId: ChatId) (buttonsGroup: ButtonsGroup) (ct: CancellationToken) =
    async { return Error "Telegram.sendButtonGroups not implemented." }
