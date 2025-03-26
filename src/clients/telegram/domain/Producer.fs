module Web.Clients.Domain.Telegram.Producer

open System

type MessageId =
    | New
    | Reply of int
    | Replace of int

type Payload<'a> =
    { ChatId: ChatId
      MessageId: MessageId
      Value: 'a }

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

type Button =
    { Name: string
      Callback: ButtonCallback }

    static member create name callback = { Name = name; Callback = callback }

type ButtonsGroup =
    { Name: string
      Columns: int
      Buttons: Button Set }

type Message =
    | Text of Payload<string>
    | ButtonsGroup of Payload<ButtonsGroup>

    static member createNew chatId (create: ChatId * MessageId -> Message) = (chatId, New) |> create

    static member tryReplace (msgId: int option) chatId (create: ChatId * MessageId -> Message) =
        match msgId with
        | Some id -> (chatId, Replace id)
        | _ -> (chatId, New)
        |> create
