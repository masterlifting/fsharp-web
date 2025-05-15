module Web.Clients.Domain.Http

open System
open System.Collections.Concurrent

type Client = Net.Http.HttpClient
type internal ClientFactory = ConcurrentDictionary<string, Client>

type Headers = Map<string, string list> option

type Connection = { Host: string; Headers: Headers }

type Request = { Path: string; Headers: Headers }

type RequestContent =
    | Bytes of byte[]
    | String of
        {|
            Data: string
            Encoding: Text.Encoding
            MediaType: string
        |}

type Response<'a> = {
    Content: 'a
    StatusCode: int
    Headers: Headers
}
