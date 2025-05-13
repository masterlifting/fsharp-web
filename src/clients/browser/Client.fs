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

            let userAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"

            let options =
                BrowserTypeLaunchOptions(
                    Headless = true, // Can be true with the additional settings below
                    Args = [|
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
                        $"--user-agent={userAgent}"
                    |]
                )

            let! browser = playwright.Chromium.LaunchAsync(options) |> Async.AwaitTask

            let contextOptions =
                BrowserNewContextOptions(
                    ViewportSize = ViewportSize(Width = 1920, Height = 1080),
                    HasTouch = false,
                    JavaScriptEnabled = true,
                    UserAgent = userAgent
                )

            let! context = browser.NewContextAsync(contextOptions) |> Async.AwaitTask

            let initScripts =
                """
                    Object.defineProperty(navigator, 'webdriver', { get: () => false });
                    Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                    Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
                """

            do! context.AddInitScriptAsync(initScripts) |> Async.AwaitTask
            let! page = context.NewPageAsync() |> Async.AwaitTask
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
