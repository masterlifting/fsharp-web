module Web.Clients.Browser.Client

open System
open Microsoft.Playwright
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Browser

let private clients = ClientFactory()

[<Literal>]
let private userAgent =
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0 Safari/537.36"

[<Literal>]
let private script =
    """
        Object.defineProperty(navigator, 'webdriver', { get: () => false });
        Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
        Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
    """

let private args = [|
    "--no-sandbox"
    "--disable-setuid-sandbox"
    "--disable-dev-shm-usage"
    "--disable-accelerated-2d-canvas"
    "--no-first-run"
    "--no-zygote"
    "--disable-gpu"
    "--hide-scrollbars"
    "--mute-audio"
    "--disable-infobars"
    "--disable-breakpad"
    "--disable-web-security"
    "--disable-extensions"
|]

let private createContext (browser: IBrowser) =
    try
        async {

            let contextOptions =
                BrowserNewContextOptions(
                    ViewportSize = ViewportSize(Width = 1920, Height = 1080),
                    HasTouch = false,
                    JavaScriptEnabled = true,
                    UserAgent = userAgent
                )

            let! context = browser.NewContextAsync contextOptions |> Async.AwaitTask

            do! context.AddInitScriptAsync script |> Async.AwaitTask

            return context |> Ok
        }
    with ex ->
        Error
        <| Operation {
            Message = "Failed to create browser context. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
        |> async.Return

let private createBrowser browserType =
    try
        async {
            let! playwright = Playwright.CreateAsync() |> Async.AwaitTask

            let options =
                BrowserTypeLaunchOptions(
                    Headless = true, // Can be true with the additional settings below
                    SlowMo = 2000.f,
                    Args = args
                )

            return!
                match browserType with
                | Chromium -> playwright.Chromium.LaunchAsync options
                | Firefox -> playwright.Firefox.LaunchAsync options
                |> Async.AwaitTask
                |> Async.map Ok
        }
    with ex ->
        Error
        <| Operation {
            Message = "Failed to create browser. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
        |> async.Return

let private cleanBrowser (client: Client) =
    async {
        try
            do! client.Browser.CloseAsync() |> Async.AwaitTask
            do! client.Browser.DisposeAsync().AsTask() |> Async.AwaitTask
            return Ok()
        with ex ->
            return
                Operation {
                    Message = "Failed to close browser. " + (ex |> Exception.toMessage)
                    Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                }
                |> Error
    }

let private createClient (connection: Connection) =
    connection.Browser
    |> createBrowser
    |> ResultAsync.bindAsync (fun browser ->
        browser
        |> createContext
        |> ResultAsync.map (fun context -> { Browser = browser; Context = context }))
    |> ResultAsync.map (fun client ->
        clients.TryAdd(connection.Browser.Value, client) |> ignore
        client)

let init (connection: Connection) =
    match clients.TryGetValue connection.Browser.Value with
    | true, client ->
        match client.Browser.Contexts.Count = 0 with
        | true ->
            clients.TryRemove connection.Browser.Value |> ignore
            cleanBrowser client |> ResultAsync.bindAsync (fun _ -> createClient connection)
        | false -> client |> Ok |> async.Return
    | _ -> connection |> createClient
