[<RequireQualifiedAccess>]
module Web.Client

open Infrastructure.Prelude

type Client =
    | Http of Http.Domain.Client.HttpClient
    | Telegram of Telegram.Domain.Client.Bot

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

let consume ct =
    ResultAsync.wrap (function
        | Consumer.Telegram(client, handle) -> client |> Telegram.Consumer.start handle ct
        | Consumer.Http client -> client |> Http.Consumer.start ct)
