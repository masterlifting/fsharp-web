[<AutoOpen>]
module Web.Clients.Domain.Telegram.Telegram

open System.Collections.Concurrent
open Infrastructure.Domain
open Telegram.Bot

type Client = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, Client>

type ChatId =
    | ChatId of int64

    member this.Value =
        match this with
        | ChatId value -> value

    member this.ValueStr = this.Value |> string

    static member parse(value: string) =
        try
            value |> int64 |> ChatId |> Ok
        with _ ->
            $"'{value}' for ChatId" |> NotSupported |> Error

type Connection = { Token: string }
