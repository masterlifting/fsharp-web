module Web.Telegram.DataAccess.Update

open Infrastructure.Domain
open Telegram.Bot.Types.Enums
open Web.Telegram.DataAccess.Message
open Web.Telegram.DataAccess.CallbackQuery


type internal Telegram.Bot.Types.Update with
    member this.ToDomain() =
        match this.Type with
        | UpdateType.Message ->
            match this.Message with
            | null -> "Message" |> NotFound |> Error
            | message -> message.ToDomain()
        | UpdateType.EditedMessage ->
            match this.EditedMessage with
            | null -> "Edited message" |> NotFound |> Error
            | editedMessage -> editedMessage.ToDomain()
        | UpdateType.ChannelPost ->
            match this.ChannelPost with
            | null -> "Channel post" |> NotFound |> Error
            | channelPost -> channelPost.ToDomain()
        | UpdateType.EditedChannelPost ->
            match this.EditedChannelPost with
            | null -> "Edited channel post" |> NotFound |> Error
            | editedChannelPost -> editedChannelPost.ToDomain()
        | UpdateType.CallbackQuery ->
            match this.CallbackQuery with
            | null -> "Callback query" |> NotFound |> Error
            | callbackQuery -> callbackQuery.ToDomain()
        | _ -> $"Update type: {this.Type}" |> NotSupported |> Error
