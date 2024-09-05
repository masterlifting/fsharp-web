module Web.Domain

type Context =
    | Http of string * Map<string, string list> option
    | Telegram of string
