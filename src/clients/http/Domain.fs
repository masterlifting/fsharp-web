module Web.Clients.Domain.Http

open System
open System.Collections.Concurrent

type Client = Net.Http.HttpClient
type internal ClientFactory = ConcurrentDictionary<string, Client>

type Headers = Map<string, string list> option

type Connection = { BaseUrl: string; Headers: Headers }

type Request = { Path: string; Headers: Headers }

type Content =
    | Bytes of byte[]
    | String of
        {|
            Data: string
            Encoding: Text.Encoding
            ContentType: string
        |}

type Response<'a> = {
    Content: 'a
    StatusCode: int
    Headers: Headers
}

module FormData =
    let build (data: Map<string, string>) =
        data
        |> Seq.map (fun x -> $"{Uri.EscapeDataString x.Key}={Uri.EscapeDataString x.Value}")
        |> String.concat "&"
