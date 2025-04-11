[<RequireQualifiedAccess>]
module Web.Client

open Web.Clients
open Web.Clients.Domain

type Provider =
    | Http of Http.Client
    | Telegram of Telegram.Client

type Connection =
    | Http of Http.Connection
    | Telegram of Telegram.Connection

let init connection =
    match connection with
    | Connection.Telegram value -> value |> Telegram.Provider.init |> Result.map Provider.Telegram
    | Connection.Http value -> value |> Http.Provider.init |> Result.map Provider.Http

type Consumer =
    | Http of Http.Client
    | Telegram of Telegram.Consumer.Handler

let consume consumer ct =
    match consumer with
    | Consumer.Telegram(client, handler) -> client |> Telegram.Consumer.start handler ct
    | Consumer.Http client -> client |> Http.Consumer.start ct
