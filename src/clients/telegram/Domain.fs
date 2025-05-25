module Web.Clients.Domain.Telegram

open System.Collections.Concurrent
open Infrastructure.Domain
open Telegram.Bot

type Client = TelegramBotClient
type internal ClientFactory = ConcurrentDictionary<string, Client>

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
            $"Chat Id '{value}' is not supported." |> NotSupported |> Error

type Connection = { Token: string }

module Consumer =

    type Payload<'a> = {
        ChatId: ChatId
        MessageId: int
        Value: 'a
    }

    type Photo = {
        FileId: string
        FileSize: int64 option
    }

    type Audio = {
        FileId: int
        FileSize: int
        Title: string
        MimeType: string
    }

    type Video = {
        FileId: int
        FileSize: int
        FileName: string
        MimeType: string
    }

    type Message =
        | Text of Payload<string>
        | Photo of Payload<Photo seq>
        | Audio of Payload<Audio>
        | Video of Payload<Video>

    type Data =
        | Message of Message
        | EditedMessage of Message
        | ChannelPost of Payload<string>
        | EditedChannelPost of Payload<string>
        | CallbackQuery of Payload<string>
        | InlineQuery of Payload<string>
        | ChosenInlineResult of Payload<string>
        | ShippingQuery of Payload<string>
        | PreCheckoutQuery of Payload<string>
        | Poll of Payload<string>
        | PollAnswer of Payload<string>
        | MyChatMember of Payload<string>
        | ChatMember of Payload<string>
        | Unknown of Payload<string>

    type Handler = Client * (Data -> Async<Result<unit, Error'>>)

module Producer =

    open System

    type MessageId =
        | New
        | Reply of int
        | Replace of int

    type Payload<'a> = {
        ChatId: ChatId
        MessageId: MessageId
        Value: 'a
    }

    [<CustomEquality; CustomComparison>]
    type ButtonCallback =
        | CallbackData of string
        | WebApp of Uri

        member this.Value =
            match this with
            | CallbackData text -> text
            | WebApp uri -> uri.AbsoluteUri

        interface IComparable with
            member this.CompareTo other =
                match other with
                | :? ButtonCallback as other -> compare this.Value other.Value
                | _ -> invalidArg "other" "Cannot compare different types"

        override this.Equals other =
            match other with
            | :? ButtonCallback as other -> this.Value = other.Value
            | _ -> false

        override this.GetHashCode() = hash this.Value

    type Button = {
        Name: string
        Callback: ButtonCallback
    } with

        static member internal create name callback = { Name = name; Callback = callback }

    type ButtonsGroup = {
        Name: string
        Columns: int
        Buttons: Button Set
    }

    type Message =
        | Text of Payload<string>
        | ButtonsGroup of Payload<ButtonsGroup>

        static member createNew chatId (create: ChatId * MessageId -> Message) = (chatId, New) |> create

        static member tryReplace (msgId: int option) chatId (create: ChatId * MessageId -> Message) =
            match msgId with
            | Some id -> (chatId, Replace id)
            | _ -> (chatId, New)
            |> create
