[<RequireQualifiedAccess>]
module Web.Client

open Infrastructure
open Web.Domain

let create context =
    match context with
    | Context.Telegram way -> Telegram.Client.create way |> Result.map Client.Telegram
    | Context.Http(url, headers) -> Http.Client.create url headers |> Result.map Client.Http

let consume ct =
    ResultAsync.wrap (function
        | Telegram(client, handle) -> client |> Telegram.Client.Consumer.start ct handle
        | Http client -> client |> Http.Client.Consumer.start ct)
