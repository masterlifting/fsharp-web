module Web.Client.Telegram

open System.Threading
open Infrastructure
open Web.Domain.Telegram.Internal


let create (token: string) : Result<Client, Error'> =
    Error <| NotImplemented "Web.Telegram.create"

let sendText (chatId: ChatId) (text: Text) (ct: CancellationToken) =
    async { return Error <| NotImplemented "Telegram.sendText." }

let sendButtonsGroups (chatId: ChatId) (buttonsGroup: ButtonsGroup) (ct: CancellationToken) =
    async { return Error "Telegram.sendButtonGroups not implemented." }
