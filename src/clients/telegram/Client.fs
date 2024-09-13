module Web.Telegram.Client

open System
open Telegram.Bot
open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain

let private clients = ClientFactory()

let private createByTokenValue token =
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

let private createByTokenEnvKey key =
    Configuration.getEnvVar key
    |> Result.bind (
        Option.map Ok
        >> Option.defaultValue (Error <| NotFound $"Environment variable '{key}'.")
    )
    |> Result.bind createByTokenValue

let create way =
    match way with
    | Value token -> createByTokenValue token
    | EnvKey key -> createByTokenEnvKey key

module private Listener =

    let private createOffset updateIds =
        updateIds |> Array.max |> (fun id -> id + 1 |> Nullable)

    let private handleTasks botId (tasks: Async<Result<unit, Error'>> array) =
        async {
            $"Telegram bot {botId} start handling messages: {tasks.Length}" |> Log.debug
            let! results = tasks |> Async.Sequential
            $"Telegram bot {botId} handled messages: {results.Length}" |> Log.debug

            results
            |> Result.unzip
            |> snd
            |> Seq.iter (fun error -> error.Message |> Log.critical)
        }
        |> Async.Start

    let listen ct receive (client: Client) =
        let limitMsg = 10
        let timeoutSec = Int32.MaxValue

        $"Telegram bot {client.BotId} started." |> Log.warning

        let rec innerLoop (offset: Nullable<int>) =
            async {
                if ct |> canceled then
                    return Error <| Canceled $"Telegram listener for {client.BotId} stopped."
                else
                    try
                        let! updates =
                            client.GetUpdatesAsync(offset, limitMsg, timeoutSec, null, ct)
                            |> Async.AwaitTask

                        let offset, tasks =
                            match updates |> Array.isEmpty with
                            | true -> offset, Array.empty
                            | false ->
                                updates
                                |> Array.map (fun item -> (item.Id, Mapper.Receive.toData item |> receive))
                                |> Array.unzip
                                |> fun (ids, tasks) -> createOffset ids, tasks

                        if tasks.Length > 0 then
                            tasks |> handleTasks client.BotId

                        return! innerLoop offset
                    with ex ->
                        return
                            Error
                            <| Operation
                                { Message = ex |> Exception.toMessage
                                  Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
            }

        let defaultInt = Nullable<int>()
        innerLoop defaultInt

module private Sender =

    let send ct (message: Send.Message) (client: Client) =
        async { return Error <| NotImplemented "Web.Telegram.Client.Send.send." }

module private Receiver =
    let receive ct (data: Receive.Data) (client: Client) =
        async { return Error <| NotImplemented "Web.Telegram.Client.Receive.receive." }

let send = Sender.send
let receive = Receiver.receive
let listen = Listener.listen
