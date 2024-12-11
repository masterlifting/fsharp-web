[<AutoOpen>]
module Web.Http.Domain.Client

open System
open System.Collections.Concurrent

type Client = Net.Http.HttpClient
type ClientFactory = ConcurrentDictionary<string, Client>

type Headers = Map<string, string list> option

type Connection = { BaseUrl: string; Headers: Headers }
