﻿module Web.Domain

open Infrastructure

type Context =
    | Http of string * Http.Domain.Headers
    | Telegram of Telegram.Domain.Token

type Client =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client

type Consumer =
    | Http of Http.Domain.Client
    | Telegram of Telegram.Domain.Client * (Telegram.Domain.Consumer.Data -> Async<Result<int, Error'>>)
