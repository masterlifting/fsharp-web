[<AutoOpen>]
module Web.Http.Domain.Request

open System
type Request = { Path: string; Headers: Headers }

type RequestContent =
    | Bytes of byte[]
    | String of
        {| Data: string
           Encoding: Text.Encoding
           MediaType: string |}
