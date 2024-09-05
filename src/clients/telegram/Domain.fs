module Web.Telegram.Domain

type Client = Telegram.Bot.TelegramBotClient

type ChatId = ChatId of string

type Text = { Id: int option; Value: string }

type Button =
    { Id: int option
      Name: string
      Value: string }

type ButtonsGroup =
    { Id: int option
      Buttons: Button seq
      Columns: int }

type Request =
    | Message of ChatId * Text
    | Buttons of ChatId * ButtonsGroup

type Response =
    | Message of Text
    | Buttons of ButtonsGroup

module External =
    open System

    type Text = { Id: Nullable<int>; Value: string }
