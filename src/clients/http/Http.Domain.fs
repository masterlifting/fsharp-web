module Web.Domain.Http

open System
open Infrastructure.Domain.Errors

type Client = Net.Http.HttpClient

type Headers = Map<string, string> option

type Request = { Path: string; Headers: Headers }

type RequestContent =
    | Bytes of byte[]
    | String of
        {| Content: string
           Encoding: Text.Encoding
           MediaType: string |}
