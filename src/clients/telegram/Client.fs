module Web.Telegram.Client

open System.Threading
open Infrastructure
open Web.Telegram.Domain


let create (token: string) : Result<Client, Error'> =
    Ok <| Client(token)

let sendText (chatId: ChatId) (text: Text) (ct: CancellationToken) =
    async { return Error <| NotImplemented "Telegram.sendText." }

let sendButtonsGroups (chatId: ChatId) (buttonsGroup: ButtonsGroup) (ct: CancellationToken) =
    async { return Error "Telegram.sendButtonGroups not implemented." }
