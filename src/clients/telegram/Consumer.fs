module Web.Telegram.Consumer

open System
open Telegram.Bot
open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain

let private createOffset updateIds =
    updateIds |> Array.max |> (fun id -> id + 1 |> Nullable)

let private handleTasks bot (tasks: Async<Result<int, Error'>> array) =
    async {
        $"{bot} Start handling messages: {tasks.Length}" |> Log.trace
        let! results = tasks |> Async.Sequential
        $"{bot} Handled messages: {results.Length}" |> Log.debug

        results
        |> Result.unzip
        |> snd
        |> Seq.iter (fun error -> bot + ". " + error.Message |> Log.critical)
    }

let start ct handle (client: Client) =
    let bot = $"Telegram bot {client.BotId}"
    let limitMsg = 10
    let restartAttempts = 5
    let timeoutSec = 60
    let defaultInt = Nullable<int>()

    $"{bot} Started." |> Log.info

    let rec innerLoop (offset: Nullable<int>) attempts =
        async {
            if ct |> canceled then
                return bot |> Canceled |> Error
            else

                if attempts <> restartAttempts then
                    $"{bot} Has been restarted." |> Log.info

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
                        tasks |> handleTasks bot |> Async.Start

                    return! innerLoop offset restartAttempts
                with ex ->
                    let error = ex |> Exception.toMessage

                    if attempts > 0 then
                        do! Async.Sleep(TimeSpan.FromSeconds 30)
                        $"{bot} Restarting... Reason: {error}" |> Log.critical
                        return! innerLoop offset (attempts - 1)
                    else
                        return
                            Error
                            <| Operation
                                { Message = bot + ". " + error
                                  Code = ErrorReason.buildLineOpt (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) }
        }

    innerLoop defaultInt restartAttempts
