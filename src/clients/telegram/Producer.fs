module Web.Clients.Telegram.Producer

open System
open Telegram.Bot
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open Telegram.Bot.Types.ReplyMarkups

module Text =

    let create (value: string) =
        fun (chatId, msgId) ->
            { MessageId = msgId
              ChatId = chatId
              Value = value }
            |> Text

    let createError (error: Error') =
        fun chatId ->
            { MessageId = New
              ChatId = chatId
              Value = error.Message }
            |> Text

module ButtonsGroup =
    let create (value: ButtonsGroup) =
        fun (chatId, msgId) ->
            { MessageId = msgId
              ChatId = chatId
              Value = value }
            |> ButtonsGroup

module private Produce =
    let private send text (markup: #IReplyMarkup option) =
        fun (chatId: ChatId) (messageId: MessageId) ct (client: Client) ->
            let chatId = chatId.Value

            match messageId with
            | New ->
                match markup with
                | Some markup -> client.SendMessage(chatId, text, replyMarkup = markup, cancellationToken = ct)
                | None -> client.SendMessage(chatId, text, cancellationToken = ct)
            | Reply messageId ->
                match markup with
                | Some markup ->
                    client.SendMessage(
                        chatId,
                        text,
                        replyParameters = messageId,
                        replyMarkup = markup,
                        cancellationToken = ct
                    )
                | None -> client.SendMessage(chatId, text, replyParameters = messageId, cancellationToken = ct)
            | Replace messageId ->
                match markup with
                | Some markup ->
                    client.EditMessageText(chatId, messageId, text, replyMarkup = markup, cancellationToken = ct)
                | None -> client.EditMessageText(chatId, messageId, text, cancellationToken = ct)

    let text (payload: Payload<string>) ct =
        fun (client: Client) ->
            async {
                try
                    let! result =
                        client
                        |> send payload.Value None payload.ChatId payload.MessageId ct
                        |> Async.AwaitTask

                    return Ok result.MessageId
                with ex ->
                    return
                        Error
                        <| Operation
                            { Message = ex |> Exception.toMessage
                              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
            }

    let buttonsGroup (payload: Payload<ButtonsGroup>) ct =
        fun (client: Client) ->

            async {
                try
                    let markup =
                        payload.Value.Buttons
                        |> Seq.chunkBySize payload.Value.Columns
                        |> Seq.map (
                            Seq.map (fun button ->
                                match button.Callback with
                                | CallbackData value -> InlineKeyboardButton.WithCallbackData(button.Name, value)
                                | WebApp value -> InlineKeyboardButton.WithWebApp(button.Name, value.AbsoluteUri))
                        )
                        |> InlineKeyboardMarkup
                        |> Some

                    let! result =
                        client
                        |> send payload.Value.Name markup payload.ChatId payload.MessageId ct
                        |> Async.AwaitTask

                    return Ok result.MessageId

                with ex ->
                    return
                        Error
                        <| Operation
                            { Message = ex |> Exception.toMessage
                              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
            }

let produce message ct =
    fun client ->
        match message with
        | Text payload -> client |> Produce.text payload ct
        | ButtonsGroup payload -> client |> Produce.buttonsGroup payload ct

let produceOk messageRes ct =
    fun client ->
        messageRes
        |> ResultAsync.bindAsync (fun message -> client |> produce message ct)

let produceResult messageRes chatId ct =
    fun client ->
        async {
            match! messageRes with
            | Ok message -> return! client |> produce message ct
            | Error error ->
                let errorMessage = Text.createError error chatId

                match! client |> produce errorMessage ct with
                | Ok _ -> return Error error
                | Error error -> return Error error
        }

let produceSeq messages ct =
    fun client ->
        messages
        |> Seq.map (fun message -> client |> produce message ct)
        |> Async.Parallel
        |> Async.map Result.choose

let produceOkSeq messagesRes ct =
    fun client ->
        messagesRes
        |> ResultAsync.bindAsync (fun messages -> client |> produceSeq messages ct)

let produceResultSeq messagesRes chatId ct =
    fun client ->
        async {
            match! messagesRes with
            | Ok messages -> return! client |> produceSeq messages ct
            | Error error ->
                let errorMessage = Text.createError error chatId

                match! client |> produce errorMessage ct with
                | Ok _ -> return Error error
                | Error error -> return Error error
        }
