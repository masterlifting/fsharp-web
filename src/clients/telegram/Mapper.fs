module Web.Telegram.Mapper

open Telegram.Bot
open Infrastructure
open Telegram.Bot.Types

//module internal Send =

module internal Receive =
    open Web.Telegram.Domain.Consumer

    let private toMessage (message: Types.Message) =
        match message.Type with
        | Enums.MessageType.Text ->
            match message.Text with
            | AP.IsString text ->
                { Id = message.MessageId
                  ChatId = message.Chat.Id
                  Value = text }
                |> Payload
                |> Ok
            | _ -> Error <| NotFound "Message text"
        | _ -> Error <| NotSupported $"Message type: {message.Type}"

    let private toCallbackQuery (query: Types.CallbackQuery) =
        match query.Data with
        | AP.IsString data ->
            { Id = query.Message.MessageId
              ChatId = query.From.Id
              Value = data }
            |> Ok
        | _ -> Error <| NotFound "Callback query data"

    let toData (update: Update) : Result<Message, Error'> =
        match update.Type with
        | Enums.UpdateType.Message -> update.Message |> toMessage |> Result.map Payload
        | Enums.UpdateType.EditedMessage -> update.EditedMessage |> toMessage |> Result.map Payload
        | Enums.UpdateType.ChannelPost -> update.ChannelPost |> toMessage |> Result.map Payload
        | Enums.UpdateType.EditedChannelPost -> update.EditedChannelPost |> toMessage |> Result.map Payload
        | Enums.UpdateType.CallbackQuery -> update.CallbackQuery |> toCallbackQuery |> Result.map CallbackQuery
        | _ -> Error <| NotSupported $"Update type: {update.Type}"
