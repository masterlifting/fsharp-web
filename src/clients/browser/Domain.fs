module Web.Clients.Domain.Browser

open System
open System.Collections.Concurrent
open Microsoft.Playwright

type Client = IPage
type ClientFactory = ConcurrentDictionary<string, Client>
type Connection = { PageUri: Uri }

type Selector =
    | Selector of string

    member this.Value =
        match this with
        | Selector selector -> selector
