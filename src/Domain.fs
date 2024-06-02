module Web.Domain

open System

module Core =
    module Bots =
        module Telegram =
            type Message = { Id: int option; Text: string }

            type Button =
                { Id: int option
                  Name: string
                  Value: string }

            type ButtonsGroup = { Id: int option; Buttons: Button seq }

module Source =
    module Bots =
        module Telegram =
            type Message =
                { Id: Nullable<int>
                  ChatId: int64
                  Text: string }
