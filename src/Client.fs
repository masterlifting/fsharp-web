[<RequireQualifiedAccess>]
module Web.Client

open Infrastructure.Prelude

type Client =
    | Http of Http.Domain.Client.Client
    | Telegram of Telegram.Domain.Client.Bot

type Connection =
    | Http of string * Http.Domain.Headers.Headers
    | Telegram of Web.Telegram.Domain.Client.Token

type Consumer =
    | Http of Http.Domain.Client.Client
    | Telegram of Telegram.Domain.Consumer.Handler

let init connection =
    match connection with
    | Connection.Telegram option -> Telegram.Client.init option |> Result.map Client.Telegram
    | Connection.Http(url, headers) -> Http.Client.init url headers |> Result.map Client.Http

let consume ct =
    ResultAsync.wrap (function
        | Consumer.Telegram(client, handle) -> client |> Telegram.Consumer.start ct handle
        | Consumer.Http client -> client |> Http.Consumer.start ct)
