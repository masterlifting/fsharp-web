module Web.Telegram.Producer

open System
open Telegram.Bot
open Infrastructure
open Web.Telegram.Domain
open System.Collections.Generic
open Web.Telegram.Domain.Producer
open Telegram.Bot.Types.ReplyMarkups

module Text =

    let create (chatId, msgId) (value: string) =
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
    let create (chatId, msgId) (value: Buttons) =
        { Id = msgId
          ChatId = chatId
          Value = value }
        |> Buttons

module private Produce =
    let send ct (dto: Dto<string>) (client: Client) =
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
                    client.SendMessage(dto.ChatId.Value, dto.Value, replyParameters = messageId, cancellationToken = ct)
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

let produce ct client =
    fun data ->
        match data with
        | Text dto -> client |> Produce.text ct dto
        | Buttons dto -> client |> Produce.buttons ct dto
        | _ -> $"{data}" |> NotSupported |> Error |> async.Return

let produceOk ct client =
    fun (dataRes: Async<Result<Data, Error'>>) -> dataRes |> ResultAsync.bindAsync (produce ct client)

let produceResult chatId ct client =
    fun (dataRes: Async<Result<Data, Error'>>) ->
        async {
            match! dataRes with
            | Ok data -> return! data |> produce ct client
            | Error error ->
                let data = Text.createError error chatId

                match! data |> produce ct client with
                | Ok _ -> return Error error
                | Error error -> return Error error
        }
