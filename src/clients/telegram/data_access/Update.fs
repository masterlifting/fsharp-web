module Web.Clients.DataAccess.Telegram.Update

open Infrastructure.Domain
open Telegram.Bot.Types.Enums
open Web.Clients.DataAccess.Telegram.Message
open Web.Clients.DataAccess.Telegram.CallbackQuery

type internal Telegram.Bot.Types.Update with
    member this.ToDomain() =
        match this.Type with
        | UpdateType.Message ->
            match this.Message with
            | null -> "Telegram 'Message' type" |> NotFound |> Error
            | message -> message.ToDomain()
        | UpdateType.EditedMessage ->
            match this.EditedMessage with
            | null -> "Telegram 'EditedMessage' type" |> NotFound |> Error
            | editedMessage -> editedMessage.ToDomain()
        | UpdateType.ChannelPost ->
            match this.ChannelPost with
            | null -> "Telegram 'ChannelPost' type" |> NotFound |> Error
            | channelPost -> channelPost.ToDomain()
        | UpdateType.EditedChannelPost ->
            match this.EditedChannelPost with
            | null -> "Telegram 'EditedChannelPost' type" |> NotFound |> Error
            | editedChannelPost -> editedChannelPost.ToDomain()
        | UpdateType.CallbackQuery ->
            match this.CallbackQuery with
            | null -> "Telegram 'CallbackQuery' type" |> NotFound |> Error
            | callbackQuery -> callbackQuery.ToDomain()
        | _ -> $"Telegram 'Update' type '{this.Type}'" |> NotSupported |> Error
