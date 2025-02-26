module Web.Telegram.Domain.Producer

open System

type DtoId =
    | New
    | Reply of int
    | Replace of int

type Payload<'a> =
    { Id: DtoId; ChatId: ChatId; Value: 'a }

[<CustomEquality; CustomComparison>]
type Key =
    | Text of string
    | Route of Uri

    member this.Value =
        match this with
        | Text t -> t
        | Route u -> u.AbsoluteUri

    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? Key as other -> compare this.Value other.Value
            | _ -> invalidArg "other" "Cannot compare different types"

    override this.Equals other =
        match other with
        | :? Key as other -> this.Value = other.Value
        | _ -> false

    override this.GetHashCode() = hash this.Value


type ButtonsGroup =
    { Name: string
      Columns: int
      Items: Map<Key, string> }

type Message =
    | Text of Payload<string>
    | Html of Payload<string>
    | ButtonsGroup of Payload<ButtonsGroup>
