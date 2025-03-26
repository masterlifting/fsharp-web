module Web.Clients.Domain.Telegram.Consumer

open Infrastructure.Domain

type Payload<'a> =
    { ChatId: ChatId
      MessageId: int
      Value: 'a }

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
