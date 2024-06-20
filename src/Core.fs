module Web.Core

open Infrastructure
open Domain

let createClient ``type`` =
    match ``type`` with
    | Type.Http baseUrl -> Http.create baseUrl |> Result.map HttpClient
    | Type.Telegram token -> Telegram.create token |> Result.map TelegramClient
