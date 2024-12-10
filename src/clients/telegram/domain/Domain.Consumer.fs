module Web.Telegram.Domain.Consumer

type Dto<'a> = { Id: int; ChatId: ChatId; Value: 'a }

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
