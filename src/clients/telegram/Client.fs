module Web.Telegram.Client

open System
open System.Threading
open Telegram.Bot
open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain

let private clients = ClientFactory()

let private create' (token: string) =
    try
        let client = new Client(token)
        Ok client
    with ex ->
        Error
        <| Operation
            { Message = ex |> Exception.toMessage
              Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }

let private createByToken token =
    match clients.TryGetValue token with
    | true, client -> Ok client
    | _ ->
        create' token
        |> Result.map (fun client ->
            clients.TryAdd(token, client) |> ignore
            client)

let private createByTokenEnvVar key =
    Configuration.getEnvVar key
    |> Result.bind (
        Option.map Ok
        >> Option.defaultValue (Error <| NotFound $"Environment variable '{key}'.")
    )
    |> Result.bind createByToken

let create way =
    match way with
    | Value token -> createByToken token
    | EnvKey key -> createByTokenEnvVar key

let listen (ct: CancellationToken) (receive: Receive.Data -> Async<Result<unit, Error'>>) (client: Client) =
    let rec innerLoop (offset: Nullable<int>) =
        async {
            if ct |> canceled then
                return
                    Error
                    <| Canceled(ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            else
                try
                    "Listening..." |> Log.warning

                    let! updates = client.GetUpdatesAsync(offset, 5, 30) |> Async.AwaitTask

                    let offset =
                        match updates |> Array.isEmpty with
                        | true -> offset
                        | false ->
                            $"Received {updates.Length} updates." |> Log.warning
                            updates |> Array.maxBy _.Id |> (fun x -> x.Id + 1 |> Nullable)

                    return! innerLoop offset
                with ex ->
                    ex |> Exception.toMessage |> Log.critical
                    return! innerLoop offset
        }

    let defaultInt = Nullable<int>()
    innerLoop defaultInt



let send ct (message: Send.Message) (client: Client) =
    async { return Error <| NotImplemented "Telegram.send." }
