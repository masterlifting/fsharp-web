module Web.Domain

open System

module Core =
    module Bots =
        module Telegram =

            type ChatId = ChatId of string

            type Text = { Id: int option; Value: string }

            type Button =
                { Id: int option
                  Name: string
                  Value: string }

            type ButtonsGroup =
                { Id: int option
                  Buttons: Button seq
                  Cloumns: int }

module Source =
    module Bots =
        module Telegram =
            type Text = { Id: Nullable<int>; Value: string }
