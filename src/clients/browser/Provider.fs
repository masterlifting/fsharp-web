module Web.Clients.Browser.Provider

open System
open Microsoft.Playwright
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Browser

let private clients = ClientFactory()

let private create () =
    try
        async {
            let! playwright = Playwright.CreateAsync() |> Async.AwaitTask
            let! browser =
                playwright.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true))
                |> Async.AwaitTask
            return browser |> Provider |> Ok
        }
    with ex ->
        Error
        <| Operation {
            Message = ex |> Exception.toMessage
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
        |> async.Return

let init (connection: Connection) =
    match clients.TryGetValue connection.Host with
    | true, client -> client |> Ok |> async.Return
    | _ ->
        create ()
        |> ResultAsync.map (fun client ->
            clients.TryAdd(connection.Host, client) |> ignore
            client)
