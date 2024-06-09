﻿module Web.Core

open System
open System.Threading
open Infrastructure
open Infrastructure.Domain.Errors

module Http =
    module Mapper =
        let toUri (url: string) =
            try
                Ok <| Uri url
            with ex ->
                Error <| Parsing ex.Message

        let toQueryParams (uri: Uri) =
            uri.Query.Split('&')
            |> Array.map (fun parameter ->
                match parameter.Split('=') with
                | parts when parts.Length = 2 -> Ok(parts.[0], parts.[1])
                | _ -> Error <| Parsing $"Invalid query parameter of '{parameter}'.")
            |> DSL.Seq.resultOrError
            |> Result.map Map

    let get (url: Uri) =
        async { return Error <| NotImplemented }

    let post (url: Uri) (data: byte[]) =
        async { return Error <| NotImplemented}

module Bots =
    module Telegram =
        open Domain.Internal.Bots.Telegram

        let sendText (chatId: ChatId) (text: Text) (ct: CancellationToken) =
            async { return Error "Telegram.sendMessage not implemented." }

        let sendButtonsGroups (chatId: ChatId) (buttonsGroup: ButtonsGroup) (ct: CancellationToken) =
            async { return Error "Telegram.sendButtonGroups not implemented." }
