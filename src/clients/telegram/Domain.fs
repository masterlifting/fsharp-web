module Web.Telegram.Domain

module Internal =
    type Client = WebClient

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
