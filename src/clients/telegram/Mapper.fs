module Web.Telegram.Mapper

open Telegram.Bot
open Infrastructure
open Telegram.Bot.Types

module internal Consumer =
    open Web.Telegram.Domain.Consumer

    let private toMessage (message: Types.Message) =
        match message.Type with
        | Enums.MessageType.Text ->
            match message.Text with
            | AP.IsString text ->
                { Id = message.MessageId
                  ChatId = message.Chat.Id |> Domain.ChatId
                  Value = text }
                |> Text
                |> Message
                |> Ok
            | _ -> Error <| NotFound "Message text"
        | _ -> Error <| NotSupported $"Message type: {message.Type}"

    let private toCallbackQuery (query: CallbackQuery) =
        match query.Data with
        | AP.IsString data ->
            { Id = query.Message.MessageId
              ChatId = query.From.Id |> Domain.ChatId
              Value = data }
            |> CallbackQuery
            |> Ok
        | _ -> Error <| NotFound "Callback query data"

    let toData (update: Update) : Result<Data, Error'> =
        match update.Type with
        | Enums.UpdateType.Message -> update.Message |> toMessage
        | Enums.UpdateType.EditedMessage -> update.EditedMessage |> toMessage
        | Enums.UpdateType.ChannelPost -> update.ChannelPost |> toMessage
        | Enums.UpdateType.EditedChannelPost -> update.EditedChannelPost |> toMessage
        | Enums.UpdateType.CallbackQuery -> update.CallbackQuery |> toCallbackQuery
        | _ -> Error <| NotSupported $"Update type: {update.Type}"
