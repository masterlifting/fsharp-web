module Web.Telegram.DataAccess.Message

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open Web.Telegram.Domain.Consumer

type internal Telegram.Bot.Types.Message with
    member this.ToDomain() =
        match this.Type with
        | Telegram.Bot.Types.Enums.MessageType.Text ->
            match this.Text with
            | AP.IsString text ->
                { Id = this.MessageId
                  ChatId = this.Chat.Id |> ChatId
                  Value = text }
                |> Text
                |> Message
                |> Ok
            | _ -> "Message text" |> NotFound |> Error
        | _ -> $"Message type: {this.Type}" |> NotSupported |> Error
