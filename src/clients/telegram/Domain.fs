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

type ChatId = ChatId of int64
type MessageId = MessageId of int
type Message<'a> = { Id: MessageId; ChatId: ChatId; Value: 'a }

type MessageType =
    | Text of Message<string>
    | Photo of Message<{|FileId: string; FileSize: int64 option|} seq>
    | Audio of Message<{|FileId:int; FileSize: int; Title: string; MimeType: string|}>
    | Video of Message<{|FileId:int; FileSize: int; FileName: string; MimeType: string|}>

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
    | Message of MessageType
    | EditedMessage of MessageType
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

type Text = { Id: int option; Value: string }

type Response =
    | Message of Text
    | Buttons of ButtonsGroup

module External =
    open System

    type Text = { Id: Nullable<int>; Value: string }
