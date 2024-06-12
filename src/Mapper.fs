module Web.Mapper

open Infrastructure.Dsl.ActivePatterns

// module Bots =
//     module Telegram =
//         let toCoreMessage (message: Domain.External.Bots.Telegram.Text) : Domain.Internal.Bots.Telegram.Text =
//             { Id = message.Id |> Option.ofNullable
//               ChatId = message.ChatId
//               Value = message.Value }

//         let toSourceMessage (message: Domain.Internal.Bots.Telegram.Text) : Domain.External.Bots.Telegram.Text =
//             { Id = message.Id |> Option.toNullable
//               ChatId = message.ChatId
//               Value = message.Value }
