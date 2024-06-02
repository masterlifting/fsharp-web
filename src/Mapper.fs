module Web.Mapper

open Infrastructure.DSL.ActivePatterns

module Bots =
    module Telegram =
        let toCoreMessage (message: Domain.Source.Bots.Telegram.Message) : Domain.Core.Bots.Telegram.Message =
            { Id = message.Id |> Option.ofNullable
              ChatId = message.ChatId
              Text = message.Text }

        let toSourceMessage (message: Domain.Core.Bots.Telegram.Message) : Domain.Source.Bots.Telegram.Message =
            { Id = message.Id |> Option.toNullable
              ChatId = message.ChatId
              Text = message.Text }
