module Web.Telegram.Domain

open System.Collections.Concurrent
open Telegram.Bot

type Client = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, Client>

type Token =
    | Value of string
    | EnvKey of string

module Producer =
    open System

    type DtoId =
        | New
        | Reply of int
        | Replace of int

    type Dto<'a> = { Id: DtoId; ChatId: int64; Value: 'a }

    type Buttons =
        { Name: string
          Columns: int
          Data: Map<string, string> }

    type WebApps =
        { Name: string
          Columns: int
          Data: Map<string, Uri> }

    type Data =
        | Text of Dto<string>
        | Html of Dto<string>
        | Buttons of Dto<Buttons>
        | WebApps of Dto<WebApps>

module Consumer =

    type Dto<'a> = { Id: int; ChatId: int64; Value: 'a }

    type Photo =
        { FileId: string
          FileSize: int64 option }

    type Audio =
        { FileId: int
          FileSize: int
          Title: string
          MimeType: string }

    type Video =
        { FileId: int
          FileSize: int
          FileName: string
          MimeType: string }

    type Message =
        | Text of Dto<string>
        | Photo of Dto<Photo seq>
        | Audio of Dto<Audio>
        | Video of Dto<Video>

    type Data =
        | Message of Message
        | EditedMessage of Message
        | ChannelPost of Dto<string>
        | EditedChannelPost of Dto<string>
        | CallbackQuery of Dto<string>
        | InlineQuery of Dto<string>
        | ChosenInlineResult of Dto<string>
        | ShippingQuery of Dto<string>
        | PreCheckoutQuery of Dto<string>
        | Poll of Dto<string>
        | PollAnswer of Dto<string>
        | MyChatMember of Dto<string>
        | ChatMember of Dto<string>
        | Unknown of Dto<string>
