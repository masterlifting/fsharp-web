[<RequireQualifiedAccess>]
module Web.Client

open Infrastructure
open Web.Domain

let create context =
    match context with
    | Context.Telegram way -> Telegram.Client.create way |> Result.map Client.Telegram
    | Context.Http(url, headers) -> Http.Client.create url headers |> Result.map Client.Http

let listen ct =
    ResultAsync.wrap (fun listener ->
        match listener with
        | Listener.Telegram(client, receive) -> client |> Telegram.Client.listen ct receive
        | Listener.Http client -> client |> Http.Client.listen ct)
