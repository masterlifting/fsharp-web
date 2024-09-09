[<RequireQualifiedAccess>]
module Web.Client

open Web

[<RequireQualifiedAccess>]
type Type =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client

let create context =
    match context with
    | Domain.Telegram way -> Telegram.Client.create way |> Result.map Type.Telegram
    | Domain.Http(url, headers) -> Http.Client.create url headers |> Result.map Type.Http

let listen ct context =
    match context with
    | Type.Telegram client -> Telegram.Client.listen ct client
    | Type.Http client -> Http.Client.listen ct client
