module Web.Telegram.Mapper

open Telegram.Bot
open Infrastructure
open Telegram.Bot.Types
open Web.Telegram.Domain

//module internal Send =

module internal Receive =
    open Web.Telegram.Domain.Receive

    let private toMessage (message: Types.Message) =
        match message.Type with
        | Enums.MessageType.Text ->
            { Id = MessageId message.MessageId
              ChatId = ChatId message.Chat.Id
              Value = message.Text }
            |> Text
            |> Ok
        | _ -> Error <| NotSupported $"Message type: {message.Type}"

    let toData (update: Update) : Result<Receive.Data, Error'> =
        match update.Type with
        | Enums.UpdateType.Message -> update.Message |> toMessage |> Result.map Message
        | _ -> Error <| NotSupported $"Update type: {update.Type}"
