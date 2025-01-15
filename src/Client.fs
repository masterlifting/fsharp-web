[<RequireQualifiedAccess>]
module Web.Client

type Client =
    | Http of Http.Domain.Client.HttpClient
    | Telegram of Telegram.Domain.Client.TelegramBot

type Connection =
    | Http of Http.Domain.Client.Connection
    | Telegram of Web.Telegram.Domain.Client.Token

let init connection =
    match connection with
    | Connection.Telegram value -> value |> Telegram.Client.init |> Result.map Client.Telegram
    | Connection.Http value -> value |> Http.Client.init |> Result.map Client.Http

type Consumer =
    | Http of Http.Domain.Client.HttpClient
    | Telegram of Telegram.Domain.Consumer.Handler

let consume consumer ct =
    match consumer with
    | Consumer.Telegram(client, handler) -> client |> Telegram.Consumer.start handler ct
    | Consumer.Http client -> client |> Http.Consumer.start ct
