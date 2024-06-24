module Web.Domain.Http

open System
open Infrastructure.Domain.Errors

type Client = Net.Http.HttpClient

type Headers = Map<string, string> option

type Request =
    | Get of string * Headers
    | Post of string * byte[] * Headers

type Response = string * Headers

