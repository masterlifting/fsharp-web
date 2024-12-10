module Web.Domain

open Infrastructure.Domain

type Context =
    | Http of string * Http.Domain.Headers
    | Telegram of Web.Telegram.Domain.Client.Token

type Client =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client.Bot

type Consumer =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client.Bot * (Telegram.Domain.Consumer.Data -> Async<Result<int, Error'>>)
