[<RequireQualifiedAccess>]
module Web.Client

open Web.Domain


let create context =
    match context with
    | Context.Telegram way -> Telegram.Client.create way |> Result.map Client.Telegram
    | Context.Http(url, headers) -> Http.Client.create url headers |> Result.map Client.Http

let listen ct listener =
    match listener with
    | Listener.Telegram listener -> Telegram.Client.listen ct client listener
    | Listener.Http client -> Http.Client.listen ct client
