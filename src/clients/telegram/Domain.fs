module Web.Telegram.Domain

open System.Collections.Concurrent
open Telegram.Bot

type Client = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, Client>

type Token =
    | Value of string
    | EnvKey of string

module Send =
    open System

    type Message<'a> =
        { Id: int option
          ChatId: int64
          Value: 'a }

    type Buttons =
        { Name: string
          Data: Map<string, string> }

    type WebApps =
        { Name: string; Data: Map<string, Uri> }

    type Data =
        | Text of Message<string>
        | Html of Message<string>
        | Buttons of Message<Buttons>
        | WebApps of Message<WebApps>

module Receive =

    type Message<'a> = { Id: int; ChatId: int64; Value: 'a }

    type DataMessage =
        | Text of Message<string>
        | Photo of
            Message<
                {| FileId: string
                   FileSize: int64 option |} seq
             >
        | Audio of
            Message<
                {| FileId: int
                   FileSize: int
                   Title: string
                   MimeType: string |}
             >
        | Video of
            Message<
                {| FileId: int
                   FileSize: int
                   FileName: string
                   MimeType: string |}
             >

    type ChannelPost = ChannelPost of Message<string>
    type InlineQuery = InlineQuery of Message<string>
    type ChosenInlineResult = ChosenInlineResult of Message<string>
    type ShippingQuery = ShippingQuery of Message<string>
    type PreCheckoutQuery = PreCheckoutQuery of Message<string>
    type Poll = Poll of Message<string>
    type PollAnswer = PollAnswer of Message<string>
    type MyChatMember = MyChatMember of Message<string>
    type ChatMember = ChatMember of Message<string>

    type Data =
        | Message of DataMessage
        | EditedMessage of DataMessage
        | ChannelPost of ChannelPost
        | EditedChannelPost of ChannelPost
        | CallbackQuery of Message<string>
        | InlineQuery of InlineQuery
        | ChosenInlineResult of ChosenInlineResult
        | ShippingQuery of ShippingQuery
        | PreCheckoutQuery of PreCheckoutQuery
        | Poll of Poll
        | PollAnswer of PollAnswer
        | MyChatMember of MyChatMember
        | ChatMember of ChatMember
        | Unknown of Message<string>
