module Web.Telegram.Mapper

open Telegram.Bot
open Infrastructure
open Telegram.Bot.Types
open Web.Telegram.Domain

//module internal Send =

module internal Receive =
    open Web.Telegram.Domain.Receive

    let toData (item: Update) =
        { Id = MessageId item.Id
          ChatId = ChatId item.Message.Chat.Id
          Value = item.Message.Text }
        |> Text
        |> Message
