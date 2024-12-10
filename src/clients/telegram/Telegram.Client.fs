module Web.Telegram.Client

open System
open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Telegram.Domain

let private clients = ClientFactory()

let private initByToken token =
    match clients.TryGetValue token with
    | true, client -> Ok client
    | _ ->
        try
            let client = Client(token)
            clients.TryAdd(token, client) |> ignore
            Ok client
        with ex ->
            Error
            <| Operation
                { Message = ex |> Exception.toMessage
                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }

let private initByTokenEnv key =
    Configuration.getEnvVar key
    |> Result.bind (
        Option.map Ok
        >> Option.defaultValue (Error <| NotFound $"Environment variable '{key}'.")
    )
    |> Result.bind initByToken

let init token =
    match token with
    | Value token -> initByToken token
    | EnvKey key -> initByTokenEnv key
