module Web.Clients.Browser.Client

open System
open Microsoft.Playwright
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Browser

let private clients = ClientFactory()

let private create (uri: Uri) =
    try
        async {
            let! playwright = Playwright.CreateAsync() |> Async.AwaitTask
            let! browser =
                playwright.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true, SlowMo = 3000.f))
                |> Async.AwaitTask

            let! page = browser.NewPageAsync() |> Async.AwaitTask

            let url = uri |> string

            match! page.GotoAsync url |> Async.AwaitTask with
            | null ->
                return
                    Error
                    <| Operation {
                        Message = $"Failed to create client '%s{url}'. Client was not created."
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
            | response ->
                match response.Status = 200 with
                | false ->
                    return
                        Error
                        <| Operation {
                            Message = $"Failed to create client '%s{url}': '%s{response.StatusText}'"
                            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                        }
                | true -> return page |> Ok
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
        create connection.PageUri
        |> ResultAsync.map (fun client ->
            clients.TryAdd(connection.PageUri.AbsoluteUri, client) |> ignore
            client)
