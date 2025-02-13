module Web.Telegram.Producer

open System
open Telegram.Bot
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open Web.Telegram.Domain.Producer
open Telegram.Bot.Types.ReplyMarkups

module Text =

    let create (value: string) =
        fun (chatId, msgId) ->
            { Id = msgId
              ChatId = chatId
              Value = value }
            |> Text

    let createError (error: Error') =
        fun chatId ->
            { Id = New
              ChatId = chatId
              Value = error.Message }
            |> Text

module Buttons =
    let create (value: Buttons) =
        fun (chatId, msgId) ->
            { Id = msgId
              ChatId = chatId
              Value = value }
            |> Buttons

module private Produce =
    let private send (msg: Dto<string>) ct =
        fun (client: TelegramBot) (markup: #IReplyMarkup option) ->
            match msg.Id with
            | New ->
                match markup with
                | Some markup ->
                    client.SendMessage(msg.ChatId.Value, msg.Value, replyMarkup = markup, cancellationToken = ct)
                | None -> client.SendMessage(msg.ChatId.Value, msg.Value, cancellationToken = ct)
            | Reply messageId ->
                match markup with
                | Some markup ->
                    client.SendMessage(
                        msg.ChatId.Value,
                        msg.Value,
                        replyParameters = messageId,
                        replyMarkup = markup,
                        cancellationToken = ct
                    )
                | None ->
                    client.SendMessage(msg.ChatId.Value, msg.Value, replyParameters = messageId, cancellationToken = ct)
            | Replace messageId ->
                match markup with
                | Some markup ->
                    client.EditMessageText(
                        msg.ChatId.Value,
                        messageId,
                        msg.Value,
                        replyMarkup = markup,
                        cancellationToken = ct
                    )
                | None -> client.EditMessageText(msg.ChatId.Value, messageId, msg.Value, cancellationToken = ct)

    let text (dto: Dto<string>) ct =
        fun (client: TelegramBot) ->
            async {
                try
                    let sendMessage = client |> send dto ct
                    let! result = sendMessage None |> Async.AwaitTask

                    return Ok result.MessageId
                with ex ->
                    return
                        Error
                        <| Operation
                            { Message = ex |> Exception.toMessage
                              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
            }

    let buttons (dto: Dto<Buttons>) ct =
        fun (client: TelegramBot) ->

            async {
                try
                    let markup =
                        dto.Value.Data
                        |> Seq.chunkBySize dto.Value.Columns
                        |> Seq.map (Seq.map (fun item -> InlineKeyboardButton.WithCallbackData(item.Value, item.Key)))
                        |> InlineKeyboardMarkup
                        |> Some

                    let msg =
                        { Id = dto.Id
                          ChatId = dto.ChatId
                          Value = dto.Value.Name }

                    let sendMessage = client |> send msg ct
                    let! result = sendMessage markup |> Async.AwaitTask

                    return Ok result.MessageId

                with ex ->
                    return
                        Error
                        <| Operation
                            { Message = ex |> Exception.toMessage
                              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
            }

let produce data ct =
    fun client ->
        match data with
        | Text dto -> client |> Produce.text dto ct
        | Buttons dto -> client |> Produce.buttons dto ct
        | _ -> $"{data}" |> NotSupported |> Error |> async.Return

let produceOk dataRes ct =
    fun client -> dataRes |> ResultAsync.bindAsync (fun data -> client |> produce data ct)

let produceResult dataRes chatId ct =
    fun client ->
        async {
            match! dataRes with
            | Ok data -> return! client |> produce data ct
            | Error error ->
                let data = Text.createError error chatId

                match! client |> produce data ct with
                | Ok _ -> return Error error
                | Error error -> return Error error
        }

let produceSeq data ct =
    fun client ->
        data
        |> Seq.map (fun message -> client |> produce message ct)
        |> Async.Parallel
        |> Async.map Result.choose

let produceOkSeq dataRes ct =
    fun client -> dataRes |> ResultAsync.bindAsync (fun data -> client |> produceSeq data ct)

let produceResultSeq dataRes chatId ct =
    fun client ->
        async {
            match! dataRes with
            | Ok data -> return! client |> produceSeq data ct
            | Error error ->
                let data = Text.createError error chatId

                match! client |> produce data ct with
                | Ok _ -> return Error error
                | Error error -> return Error error
        }
