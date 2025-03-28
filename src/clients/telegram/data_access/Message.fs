module Web.Clients.DataAccess.Telegram.Message

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Consumer

type internal Telegram.Bot.Types.Message with
    member this.ToDomain() =
        match this.Type with
        | Telegram.Bot.Types.Enums.MessageType.Text ->
            match this.Text with
            | AP.IsString text ->
                {
                    MessageId = this.MessageId
                    ChatId = this.Chat.Id |> ChatId
                    Value = text
                }
                |> Text
                |> Message
                |> Ok
            | _ -> "Telegram 'Message' text" |> NotFound |> Error
        | _ -> $"Telegram 'Message' type '{this.Type}'" |> NotSupported |> Error
