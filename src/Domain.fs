module Web.Domain

type Type =
    | Http of string
    | Telegram of string

type WebClient =
    | HttpClient of Http.Client
    | TelegramClient of Telegram.Client

type Request =
    | Http of Http.Domain.Request
    | Telegram of Telegram.Domain.Internal.Request

type Response =
    | Http of Http.Domain.Response
    | Telegram of Telegram.Domain.Internal.Response
