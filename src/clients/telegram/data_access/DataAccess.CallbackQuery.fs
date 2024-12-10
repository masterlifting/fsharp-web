﻿module Web.Telegram.DataAccess.CallbackQuery

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open Web.Telegram.Domain.Consumer

type internal Telegram.Bot.Types.CallbackQuery with
    member this.ToDomain() =
        match this.Data with
        | AP.IsString data ->
            { Id = this.Message.MessageId
              ChatId = this.From.Id |> ChatId
              Value = data }
            |> CallbackQuery
            |> Ok
        | _ -> "Callback query data" |> NotFound |> Error