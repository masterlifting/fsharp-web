module Web.Clients.Domain.Browser

open System
open System.Collections.Concurrent
open Microsoft.Playwright

type Client = {
    Browser: IBrowser
    Context: IBrowserContext
}
type Page = IPage
type internal ClientFactory = ConcurrentDictionary<string, Client>

type BrowserType =
    | Chromium
    | Firefox

    member this.Value =
        match this with
        | Chromium -> "chromium"
        | Firefox -> "firefox"

type Connection = { Browser: BrowserType }

type Selector =
    | Selector of string

    member this.Value =
        match this with
        | Selector selector -> selector

module Mouse =

    type WaitFor =
        | Url of string
        | Selector of Selector
        | Nothing
