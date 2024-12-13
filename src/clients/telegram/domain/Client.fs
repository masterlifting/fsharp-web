[<AutoOpen>]
module Web.Telegram.Domain.Client

open System.Collections.Concurrent
open Telegram.Bot

type TelegramBot = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, TelegramBot>

type Token =
    | Value of string
    | EnvKey of string

type ChatId =
    | ChatId of int64

    member this.Value =
        match this with
        | ChatId value -> value
