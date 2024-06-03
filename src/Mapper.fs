module Web.Mapper

open Infrastructure.DSL.ActivePatterns

module Bots =
    module Telegram =
        let toCoreMessage (message: Domain.Source.Bots.Telegram.Text) : Domain.Core.Bots.Telegram.Text =
            { Id = message.Id |> Option.ofNullable
              ChatId = message.ChatId
              Value = message.Value }

        let toSourceMessage (message: Domain.Core.Bots.Telegram.Text) : Domain.Source.Bots.Telegram.Text =
            { Id = message.Id |> Option.toNullable
              ChatId = message.ChatId
              Value = message.Value }
