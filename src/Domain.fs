module Web.Domain

open Infrastructure

type Context =
    | Http of string * Http.Domain.Headers
    | Telegram of Telegram.Domain.CreateBy

type Client =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client

type Listener =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client * (Telegram.Domain.Listener -> Async<Result<unit, Error'>>)
