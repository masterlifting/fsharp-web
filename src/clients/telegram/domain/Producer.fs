module Web.Telegram.Domain.Producer

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

type ButtonsGroup =
    { Name: string
      Columns: int
      Items: Button Set }

type Message =
    | Text of Payload<string>
    | ButtonsGroup of Payload<ButtonsGroup>
