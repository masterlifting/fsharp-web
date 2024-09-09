module Web.Telegram.Client

open System
open System.Threading
open Telegram.Bot
open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain
open Telegram.Bot.Args
open Telegram.Bot.Types.Enums
open Telegram.Bot.Types.Enums

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

let listen (ct: CancellationToken) (listener: Domain.Listener -> Async<Result<unit, Error'>>) (client: Client) =
    async {
        let rec innerLoop (offset: Nullable<int>) =
            async {
                if ct |> canceled then
                    return
                        Error
                        <| Canceled(ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                else
                    try
                        let! updates = client.GetUpdatesAsync(offset, 5) |> Async.AwaitTask

                        let tasks =
                            updates
                            |> Array.map (fun update ->
                                match update.Type with
                                | UpdateType.Message ->
                                    match update.Message.Type with
                                    | MessageType.Text ->
                                        { Id = MessageId update.Message.MessageId
                                          ChatId = ChatId update.Message.Chat.Id
                                          Value = update.Message.Text }
                                        |> Text
                                        |> Listener.Message
                                        |> listener
                                    | MessageType.Photo ->
                                        { Id = MessageId update.Message.MessageId
                                          ChatId = ChatId update.Message.Chat.Id
                                          Value =
                                            update.Message.Photo
                                            |> Array.map (fun photo ->
                                                {| FileId = photo.FileId
                                                   FileSize = photo.FileSize |> Option.ofNullable |})
                                            |> Seq.ofArray }
                                        |> Photo
                                        |> Listener.Message
                                        |> listener
                                | UpdateType.EditedMessage ->
                                    update.EditedMessage |> Listener.EditedMessage |> listener
                                | UpdateType.ChannelPost -> update.ChannelPost |> ignore
                                | UpdateType.EditedChannelPost -> update.EditedChannelPost |> ignore
                                | UpdateType.CallbackQuery -> update.CallbackQuery |> ignore
                                | UpdateType.InlineQuery -> update.InlineQuery |> ignore
                                | UpdateType.ChosenInlineResult -> update.ChosenInlineResult |> ignore
                                | UpdateType.ShippingQuery -> update.ShippingQuery |> ignore
                                | UpdateType.PreCheckoutQuery -> update.PreCheckoutQuery |> ignore
                                | UpdateType.Poll -> update.Poll |> ignore
                                | UpdateType.PollAnswer -> update.PollAnswer |> ignore
                                | UpdateType.MyChatMember -> update.MyChatMember |> ignore
                                | UpdateType.ChatMember -> update.ChatMember |> ignore
                                | UpdateType.Unknown -> ()
                                | _ -> ())

                        return! innerLoop (updates |> Array.map (fun update -> update.Id) |> Array.max)
                    with ex ->
                        ex |> Exception.toMessage |> Log.critical
                        return! innerLoop offset
            }

        return Error <| NotImplemented "Telegram.listen."
    }

let sendText (chatId: ChatId) (text: Text) (ct: CancellationToken) =
    async { return Error <| NotImplemented "Telegram.sendText." }

let sendButtonsGroups (chatId: ChatId) (buttonsGroup: ButtonsGroup) (ct: CancellationToken) =
    async { return Error "Telegram.sendButtonGroups not implemented." }
