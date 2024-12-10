[<RequireQualifiedAccess>]
module Web.Client

open Infrastructure.Prelude
open Web.Domain

let init context =
    match context with
    | Context.Telegram option -> Telegram.Client.init option |> Result.map Client.Telegram
    | Context.Http(url, headers) -> Http.Client.create url headers |> Result.map Client.Http

let consume ct =
    ResultAsync.wrap (function
        | Telegram(client, handle) -> client |> Telegram.Consumer.start ct handle
        | Http client -> client |> Http.Client.Consumer.start ct)
