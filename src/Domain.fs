module Web.Domain

type Context =
    | Http of string * Http.Domain.Headers
    | Telegram of Telegram.Domain.CreateBy

type Response<'a> = 'a
