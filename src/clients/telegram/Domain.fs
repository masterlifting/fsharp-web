module Web.Telegram.Domain

open System.Collections.Concurrent
open Telegram.Bot

type Client = TelegramBotClient
type ClientFactory = ConcurrentDictionary<string, Client>

type Token =
    | Value of string
    | EnvKey of string

type ChatId = ChatId of int64

type MessageId =
    | MessageId of int

    static member Default = MessageId 0

type Message<'a> =
    { Id: MessageId
      ChatId: ChatId
      Value: 'a }
    
module Send =
    open System

    type Buttons =
        { Name: string
          Data: Map<string, string> }

    type WebApps =
        { Name: string; Data: Map<string, Uri> }

    type Message =
        | Text of Message<string>
        | Html of Message<string>
        | Buttons of Message<Buttons>
        | WebApps of Message<WebApps>

module Receive =

    type Message =
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
    type CallbackQuery = CallbackQuery of Message<string>
    type InlineQuery = InlineQuery of Message<string>
    type ChosenInlineResult = ChosenInlineResult of Message<string>
    type ShippingQuery = ShippingQuery of Message<string>
    type PreCheckoutQuery = PreCheckoutQuery of Message<string>
    type Poll = Poll of Message<string>
    type PollAnswer = PollAnswer of Message<string>
    type MyChatMember = MyChatMember of Message<string>
    type ChatMember = ChatMember of Message<string>

    type Data =
        | Message of Message
        | EditedMessage of Message
        | ChannelPost of ChannelPost
        | EditedChannelPost of ChannelPost
        | CallbackQuery of CallbackQuery
        | InlineQuery of InlineQuery
        | ChosenInlineResult of ChosenInlineResult
        | ShippingQuery of ShippingQuery
        | PreCheckoutQuery of PreCheckoutQuery
        | Poll of Poll
        | PollAnswer of PollAnswer
        | MyChatMember of MyChatMember
        | ChatMember of ChatMember
        | Unknown of Message<string>
