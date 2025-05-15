module Web.Clients.Domain.Browser

open System
open System.Collections.Concurrent
open Microsoft.Playwright

type Client = IBrowserContext
type Page = IPage
type ClientFactory = ConcurrentDictionary<string, Client>

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
