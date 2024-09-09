module Web.Telegram.Domain

open System.Collections.Concurrent
open Telegram.Bot

type Client = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, Client>

type CreateBy =
    | Token of string
    | TokenEnvVar of string

type ChatId = ChatId of string

type Text = { Id: int option; Value: string }

type Button =
    { Id: int option
      Name: string
      Value: string }

type ButtonsGroup =
    { Id: int option
      Buttons: Button seq
      Columns: int }

type Request =
    | Message of ChatId * Text
    | Buttons of ChatId * ButtonsGroup

type Response =
    | Message of Text
    | Buttons of ButtonsGroup

module External =
    open System

    type Text = { Id: Nullable<int>; Value: string }
