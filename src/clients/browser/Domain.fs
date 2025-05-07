module Web.Clients.Domain.Browser

open System.Collections.Concurrent
open Microsoft.Playwright

type Provider =
    | Provider of IBrowser

    member this.Value =
        match this with
        | Provider client -> client

type Page =
    | Page of IPage

    member this.Value =
        match this with
        | Page page -> page

type ClientFactory = ConcurrentDictionary<string, Provider>
type Connection = { Host: string }

type Selector =
    | Selector of string

    member this.Value =
        match this with
        | Selector selector -> selector
