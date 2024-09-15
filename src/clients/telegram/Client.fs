module Web.Telegram.Client

open System
open Telegram.Bot
open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain


let private clients = ClientFactory()

let private createByTokenValue token =
    match clients.TryGetValue token with
    | true, client -> Ok client
    | _ ->
        try
            let client = Client(token)
            clients.TryAdd(token, client) |> ignore
            Ok client
        with ex ->
            Error
            <| Operation
                { Message = ex |> Exception.toMessage
                  Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }

let private createByTokenEnvKey key =
    Configuration.getEnvVar key
    |> Result.bind (
        Option.map Ok
        >> Option.defaultValue (Error <| NotFound $"Environment variable '{key}'.")
    )
    |> Result.bind createByTokenValue

module private Listener =

    let private createOffset updateIds =
        updateIds |> Array.max |> (fun id -> id + 1 |> Nullable)

    let private handleTasks botId (tasks: Async<Result<unit, Error'>> array) =
        async {
            $"Telegram bot {botId} start handling messages: {tasks.Length}" |> Log.trace
            let! results = tasks |> Async.Sequential
            $"Telegram bot {botId} handled messages: {results.Length}" |> Log.debug

            results
            |> Result.unzip
            |> snd
            |> Seq.iter (fun error -> error.Message |> Log.critical)
        }

    let listen ct (receive: Receive.Data -> Async<Result<unit, Error'>>) (client: Client) =
        let limitMsg = 10
        let timeoutSec = Int32.MaxValue

        $"Telegram bot {client.BotId} started." |> Log.warning

        let rec innerLoop (offset: Nullable<int>) =
            async {
                if ct |> canceled then
                    return Error <| Canceled $"Telegram bot {client.BotId} stopped."
                else
                    try
                        let! updates =
                            client.GetUpdatesAsync(offset, limitMsg, timeoutSec, null, ct)
                            |> Async.AwaitTask

                        let offset, tasks =
                            match updates |> Array.isEmpty with
                            | true -> offset, Array.empty
                            | false ->
                                updates
                                |> Array.map (fun update ->
                                    let task = update |> Mapper.Receive.toData |> ResultAsync.wrap receive
                                    update.Id, task)
                                |> Array.unzip
                                |> fun (ids, tasks) -> createOffset ids, tasks

                        if tasks.Length > 0 then
                            tasks |> handleTasks client.BotId |> Async.Start

                        return! innerLoop offset
                    with ex ->
                        return
                            Error
                            <| Operation
                                { Message = ex |> Exception.toMessage
                                  Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
            }

        let defaultInt = Nullable<int>()
        innerLoop defaultInt

module private Sender =
    open System.Collections.Generic
    open Web.Telegram.Domain.Send
    open Telegram.Bot.Types.ReplyMarkups

    let private sentText ct (message: Message<string>) (client: Client) =
        async {
            try
                let! result =
                    client.SendTextMessageAsync(message.ChatId, message.Value, cancellationToken = ct)
                    |> Async.AwaitTask

                return Ok result.MessageId
            with ex ->
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
        }

    let private toColumnedMarkup columns toResult data =
        data
        |> Seq.chunkBySize columns
        |> Seq.map (Seq.map toResult)
        |> InlineKeyboardMarkup

    let private sentButtons ct (message: Message<Buttons>) (client: Client) =
        async {
            try
                let toCallbackData (item: KeyValuePair<string, string>) =
                    InlineKeyboardButton.WithCallbackData(item.Value, item.Key)

                let markup =
                    message.Value.Data |> toColumnedMarkup message.Value.Columns toCallbackData

                let! result =
                    client.SendTextMessageAsync(
                        message.ChatId,
                        message.Value.Name,
                        replyMarkup = markup,
                        cancellationToken = ct
                    )
                    |> Async.AwaitTask

                return Ok result.MessageId

            with ex ->
                return
                    Error
                    <| Operation
                        { Message = ex |> Exception.toMessage
                          Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
        }

    let send ct data client =
        match data with
        | Text msg -> client |> sentText ct msg
        | Buttons msg -> client |> sentButtons ct msg
        | _ -> async { return Error <| NotSupported $"Message type: {data}" }

module private Receiver =
    let receive ct (data: Receive.Data) (client: Client) =
        async { return Error <| NotImplemented "Web.Telegram.Client.Receive.receive." }

let create token =
    match token with
    | Value token -> createByTokenValue token
    | EnvKey key -> createByTokenEnvKey key

let send = Sender.send
let receive = Receiver.receive
let listen = Listener.listen
