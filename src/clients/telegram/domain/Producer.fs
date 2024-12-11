module Web.Telegram.Domain.Producer

open System

type DtoId =
    | New
    | Reply of int
    | Replace of int

type Dto<'a> =
    { Id: DtoId; ChatId: ChatId; Value: 'a }

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
