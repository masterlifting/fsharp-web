[<AutoOpen>]
module Web.Http.Domain.Client

open System
open System.Collections.Concurrent

type HttpClient = Net.Http.HttpClient
type ClientFactory = ConcurrentDictionary<string, HttpClient>

type Headers = Map<string, string list> option

type Connection = { BaseUrl: string; Headers: Headers }
