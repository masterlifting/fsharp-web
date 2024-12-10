[<RequireQualifiedAccess>]
module Web.Client

open Infrastructure.Domain
open Infrastructure.Prelude

type Type =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client.Bot

type Connection =
    | Http of string * Http.Domain.Headers
    | Telegram of Web.Telegram.Domain.Client.Token

type Consumer =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Consumer.Handler

let init connection =
    match connection with
    | Connection.Telegram option -> Telegram.Client.init option |> Result.map Type.Telegram
    | Connection.Http(url, headers) -> Http.Client.init url headers |> Result.map Type.Http

let consume ct =
    ResultAsync.wrap (function
        | Consumer.Telegram(client, handle) -> client |> Telegram.Consumer.start ct handle
        | Consumer.Http client -> client |> Http.Client.Consumer.start ct)
