module Web.Clients.Browser.Client

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
                playwright.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = false))
                |> Async.AwaitTask

            let! page = browser.NewPageAsync() |> Async.AwaitTask

            return page |> Ok
        }
    with ex ->
        Error
        <| Operation {
            Message = ex |> Exception.toMessage
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
        |> async.Return

let init (connection: Connection) =
    match clients.TryGetValue connection.PageUri.AbsoluteUri with
    | true, client -> client |> Ok |> async.Return
    | _ ->
        create ()
        |> ResultAsync.map (fun client ->
            clients.TryAdd(connection.PageUri.AbsoluteUri, client) |> ignore
            client)
