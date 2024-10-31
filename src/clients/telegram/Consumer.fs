module Web.Telegram.Consumer

open System
open Telegram.Bot
open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain

let private createOffset updateIds =
    updateIds |> Array.max |> (fun id -> id + 1 |> Nullable)

let private handleTasks botId (tasks: Async<Result<int, Error'>> array) =
    async {
        $"Telegram bot {botId} start handling messages: {tasks.Length}" |> Log.trace
        let! results = tasks |> Async.Sequential
        $"Telegram bot {botId} handled messages: {results.Length}" |> Log.debug

        results
        |> Result.unzip
        |> snd
        |> Seq.iter (fun error -> $"Telegram bot {botId} " + error.Message |> Log.critical)
    }

let start ct (handle: Consumer.Data -> Async<Result<int, Error'>>) (client: Client) =
    let limitMsg = 10
    let timeoutSec = Int32.MaxValue

    $"Telegram bot {client.BotId} started." |> Log.info

    let rec innerLoop (offset: Nullable<int>) =
        async {
            if ct |> canceled then
                return Error <| Canceled $"Telegram bot {client.BotId} stopped."
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
                            |> Array.map (fun update ->
                                let task = update |> Mapper.Consumer.toData |> ResultAsync.wrap handle
                                update.Id, task)
                            |> Array.unzip
                            |> fun (ids, tasks) -> createOffset ids, tasks

                    if tasks.Length > 0 then
                        tasks |> handleTasks client.BotId |> Async.Start

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
