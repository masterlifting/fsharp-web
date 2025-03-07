[<RequireQualifiedAccess>]
module Web.Client

type Type =
    | Http of Http.Domain.Client.Client
    | Telegram of Telegram.Domain.Client.Client

type Connection =
    | Http of Http.Domain.Client.Connection
    | Telegram of Telegram.Domain.Client.Connection

let init connection =
    match connection with
    | Connection.Telegram value -> value |> Telegram.Client.init |> Result.map Type.Telegram
    | Connection.Http value -> value |> Http.Client.init |> Result.map Type.Http

type Consumer =
    | Http of Http.Domain.Client.Client
    | Telegram of Telegram.Domain.Consumer.Handler

let consume consumer ct =
    match consumer with
    | Consumer.Telegram(client, handler) -> client |> Telegram.Consumer.start handler ct
    | Consumer.Http client -> client |> Http.Consumer.start ct
