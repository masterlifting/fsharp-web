module Web.Telegram

open System.Threading
open Infrastructure.Domain.Errors

open System.Net

module Domain =
    module Internal =

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

        type Request =
            | Message of ChatId * Text
            | Buttons of ChatId * ButtonsGroup

        type Response =
            | Message of Text
            | Buttons of ButtonsGroup

    module External =
        open System

        type Text = { Id: Nullable<int>; Value: string }


type Client = WebClient
open Domain

let internal create (token: string) : Result<Client, ApiError> =
    Error(Logical(NotImplemented "Web.Telegram.create"))

let sendText (chatId: Internal.ChatId) (text: Internal.Text) (ct: CancellationToken) =
    async { return Error(Logical(NotImplemented "Telegram.sendText.")) }

let sendButtonsGroups (chatId: Internal.ChatId) (buttonsGroup: Internal.ButtonsGroup) (ct: CancellationToken) =
    async { return Error "Telegram.sendButtonGroups not implemented." }
