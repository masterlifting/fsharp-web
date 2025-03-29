module Web.Clients.Telegram.Consumer

open System
open Telegram.Bot
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Web.Clients.Domain.Telegram
open Web.Clients.DataAccess.Telegram.Update

let private createOffset updateIds =
    updateIds |> Array.max |> (fun id -> id + 1 |> Nullable)

let private handleTasks bot (tasks: Async<Result<unit, Error'>> array) =
    async {
        $"{bot} start handling messages: {tasks.Length}" |> Log.trace
        let! results = tasks |> Async.Sequential
        $"{bot} finish handling messages: {results.Length}" |> Log.trace

        results
        |> Result.unzip
        |> snd
        |> Seq.iter (fun error -> bot + " got the error: " + error.Message |> Log.critical)
    }

let start handler ct =
    fun (client: Client) ->
        let bot = $"Telegram bot '{client.BotId}'"
        let limitMsg = 10
        let restartAttempts = 50
        let timeoutSec = 60
        let defaultInt = Nullable<int>()

        $"{bot} has started." |> Log.info

        let rec innerLoop (offset: Nullable<int>) attempts =
            async {
                if ct |> canceled then
                    return $"{bot} loop canceled." |> Canceled |> Error
                else

                    if attempts <> restartAttempts then
                        $"{bot} has restarted." |> Log.info

                    try
                        let! updates = client.GetUpdates(offset, limitMsg, timeoutSec, null, ct) |> Async.AwaitTask

                        let offset, tasks =
                            match updates |> Array.isEmpty with
                            | true -> offset, Array.empty
                            | false ->
                                updates
                                |> Array.map (fun update ->
                                    let task = update.ToDomain() |> ResultAsync.wrap handler
                                    update.Id, task)
                                |> Array.unzip
                                |> fun (ids, tasks) -> createOffset ids, tasks

                        if tasks.Length > 0 then
                            tasks |> handleTasks bot |> Async.Start

                        return! innerLoop offset restartAttempts
                    with ex ->
                        let error = ex |> Exception.toMessage

                        if attempts > 0 then
                            let interval = 10000.0 * Math.Pow(1.2, float (restartAttempts - attempts)) |> int
                            do! Async.Sleep interval
                            $"{bot} is restarting... %s{error}" |> Log.critical
                            return! innerLoop offset (attempts - 1)
                        else
                            return
                                Error
                                <| Operation {
                                    Message = bot + " got the error. " + error
                                    Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                                }
            }

        innerLoop defaultInt restartAttempts
