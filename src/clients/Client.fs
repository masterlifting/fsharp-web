[<RequireQualifiedAccess>]
module Web.Client

open Web

[<RequireQualifiedAccess>]
type Type =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client

let create context =
    match context with
    | Domain.Http(url, headers) -> Http.Client.create url headers |> Result.map Type.Http
    | Domain.Telegram token -> Telegram.Client.create token |> Result.map Type.Telegram
