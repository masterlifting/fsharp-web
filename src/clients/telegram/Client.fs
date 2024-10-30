module Web.Telegram.Client

open System
open Infrastructure
open Web.Telegram.Domain

let private clients = ClientFactory()

let private createByToken token =
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
                  Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }

let private createByTokenEnv key =
    Configuration.getEnvVar key
    |> Result.bind (
        Option.map Ok
        >> Option.defaultValue (Error <| NotFound $"Environment variable '{key}'.")
    )
    |> Result.bind createByToken

let create token =
    match token with
    | Value token -> createByToken token
    | EnvKey key -> createByTokenEnv key
