module Web.Telegram.DataAccess.Update

open Infrastructure.Domain
open Web.Telegram.DataAccess.Message
open Web.Telegram.DataAccess.CallbackQuery

type internal Telegram.Bot.Types.Update with
    member this.ToDomain() =
        match this.Type with
        | Telegram.Bot.Types.Enums.UpdateType.Message -> this.Message.ToDomain()
        | Telegram.Bot.Types.Enums.UpdateType.EditedMessage -> this.EditedMessage.ToDomain()
        | Telegram.Bot.Types.Enums.UpdateType.ChannelPost -> this.ChannelPost.ToDomain()
        | Telegram.Bot.Types.Enums.UpdateType.EditedChannelPost -> this.EditedChannelPost.ToDomain()
        | Telegram.Bot.Types.Enums.UpdateType.CallbackQuery -> this.CallbackQuery.ToDomain()
        | _ -> $"Update type: {this.Type}" |> NotSupported |> Error
