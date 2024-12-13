module Web.Telegram.Producer

open System
open Telegram.Bot
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open System.Collections.Generic
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
    let private send (dto: Dto<string>) ct =
        fun (client: TelegramBot) ->
            match dto.Id with
            | New ->
                fun markup ->
                    match markup with
                    | Some markup ->
                        client.SendMessage(dto.ChatId.Value, dto.Value, replyMarkup = markup, cancellationToken = ct)
                    | None -> client.SendMessage(dto.ChatId.Value, dto.Value, cancellationToken = ct)
            | Reply messageId ->

                fun markup ->
                    match markup with
                    | Some markup ->
                        client.SendMessage(
                            dto.ChatId.Value,
                            dto.Value,
                            replyParameters = messageId,
                            replyMarkup = markup,
                            cancellationToken = ct
                        )
                    | None ->
                        client.SendMessage(
                            dto.ChatId.Value,
                            dto.Value,
                            replyParameters = messageId,
                            cancellationToken = ct
                        )
            | Replace messageId ->

                fun markup ->
                    match markup with
                    | Some markup ->
                        client.EditMessageText(
                            dto.ChatId.Value,
                            messageId,
                            dto.Value,
                            replyMarkup = markup,
                            cancellationToken = ct
                        )
                    | None -> client.EditMessageText(dto.ChatId.Value, messageId, dto.Value, cancellationToken = ct)

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

                    let sendMessage = client |> send dto ct

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
