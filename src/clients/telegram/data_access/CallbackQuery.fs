module Web.Clients.DataAccess.Telegram.CallbackQuery

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Consumer

type internal Telegram.Bot.Types.CallbackQuery with
    member this.ToDomain() =
        match this.Data with
        | AP.IsString data ->
            match this.Message with
            | null -> "Telegram 'CallbackQuery' message" |> NotFound |> Error
            | message ->
                {
                    MessageId = message.MessageId
                    ChatId = this.From.Id |> ChatId
                    Value = data
                }
                |> CallbackQuery
                |> Ok
        | _ -> "Telegram 'CallbackQuery' data" |> NotFound |> Error
