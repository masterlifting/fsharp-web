module Web.Core

open Infrastructure
open Domain

let createClient ``type`` =
    match ``type`` with
    | Http baseUrl -> Http.create baseUrl |> Result.map HttpClient
    | Telegram config -> Telegram.create config |> Result.map TelegramClient
