module Web.Telegram.Domain

open System.Collections.Concurrent
open Infrastructure
open Telegram.Bot
open System

type Client = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, Client>

type CreateBy =
    | Token of string
    | TokenEnvVar of string

type Message<'a> = { Id: int; ChatId: int64; Value: 'a }

type Button =
    { Id: int option
      Name: string
      Value: string }

type ButtonsGroup =
    { Id: int option
      Buttons: Button seq
      Columns: int }

type Send =
    | Message of Message<string>
    | Buttons of ChatId * ButtonsGroup

type Listener =
    | Message of Message<string>
    | Photo of Message<(float * float) seq>

type ChatId = ChatId of string

type Text = { Id: int option; Value: string }



type Response =
    | Message of Text
    | Buttons of ButtonsGroup

module External =
    open System

    type Text = { Id: Nullable<int>; Value: string }
