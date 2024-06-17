module Web.Domain

type Type =
    | Http of string
    | Telegram of string

type WebClient =
    | HttpClient of Http.Client
    | TelegramClient of Telegram.Client
