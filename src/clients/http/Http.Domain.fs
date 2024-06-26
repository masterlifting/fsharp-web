module Web.Domain.Http

open System

type Client = Net.Http.HttpClient

type Headers = Map<string, string> option

type Request = { Path: string; Headers: Headers }

type RequestContent =
    | Bytes of byte[]
    | String of
        {| Data: string
           Encoding: Text.Encoding
           MediaType: string |}
