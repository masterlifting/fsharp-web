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

let create token =
    match token with
    | Value token -> createByTokenValue token
    | EnvKey key -> createByTokenEnvKey key

module Consumer =

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

    let start ct (handle: Consumer.Data -> Async<Result<unit, Error'>>) (client: Client) =
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
                                    let task = update |> Mapper.Consumer.toData |> ResultAsync.wrap handle
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

module Producer =
    open System.Collections.Generic
    open Web.Telegram.Domain.Producer
    open Telegram.Bot.Types.ReplyMarkups

    let private send ct (dto: Dto<string>) (client: Client) =
        match dto.Id with
        | New ->
            fun markup ->
                match markup with
                | Some markup ->
                    client.SendTextMessageAsync(dto.ChatId.Value, dto.Value, replyMarkup = markup, cancellationToken = ct)
                | None -> client.SendTextMessageAsync(dto.ChatId.Value, dto.Value, cancellationToken = ct)
        | Reply id ->
            let messageId = id |> Nullable

            fun markup ->
                match markup with
                | Some markup ->
                    client.SendTextMessageAsync(
                        dto.ChatId.Value,
                        dto.Value,
                        replyToMessageId = messageId,
                        replyMarkup = markup,
                        cancellationToken = ct
                    )
                | None ->
                    client.SendTextMessageAsync(
                        dto.ChatId.Value,
                        dto.Value,
                        replyToMessageId = messageId,
                        cancellationToken = ct
                    )
        | Replace messageId ->

            fun markup ->
                match markup with
                | Some markup ->
                    client.EditMessageTextAsync(
                        dto.ChatId.Value,
                        messageId,
                        dto.Value,
                        replyMarkup = markup,
                        cancellationToken = ct
                    )
                | None -> client.EditMessageTextAsync(dto.ChatId.Value, messageId, dto.Value, cancellationToken = ct)

    module private Produce =

        let text ct (dto: Dto<string>) (client: Client) =
            async {
                try
                    let sendMessage = client |> send ct dto

                    let! result = sendMessage None |> Async.AwaitTask

                    return Ok result.MessageId
                with ex ->
                    return
                        Error
                        <| Operation
                            { Message = ex |> Exception.toMessage
                              Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
            }

        let buttons ct (dto: Dto<Buttons>) (client: Client) =

            let inline toColumnedMarkup columns toResult data =
                data
                |> Seq.chunkBySize columns
                |> Seq.map (Seq.map toResult)
                |> InlineKeyboardMarkup

            async {
                try
                    let toCallbackData (item: KeyValuePair<string, string>) =
                        InlineKeyboardButton.WithCallbackData(item.Value, item.Key)

                    let markup =
                        dto.Value.Data |> toColumnedMarkup dto.Value.Columns toCallbackData |> Some

                    let dto =
                        { Id = dto.Id
                          ChatId = dto.ChatId
                          Value = dto.Value.Name }

                    let sendMessage = client |> send ct dto

                    let! result = sendMessage markup |> Async.AwaitTask

                    return Ok result.MessageId

                with ex ->
                    return
                        Error
                        <| Operation
                            { Message = ex |> Exception.toMessage
                              Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
            }

    let produce ct data client =
        match data with
        | Text dto -> client |> Produce.text ct dto
        | Buttons dto -> client |> Produce.buttons ct dto
        | _ -> async { return Error <| NotSupported $"Message type: {data}" }
