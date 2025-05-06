module Web.Clients.Browser.Provider

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Http

let private clients = ClientFactory()

let private create (baseUrl: Uri) =
    try
        let client = new Client()
        client.BaseAddress <- baseUrl
        Ok client
    with ex ->
        Error
        <| Operation {
            Message = ex |> Exception.toMessage
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }

let init (connection: Connection) =
    "Browser.Provider.init is not implemented" |> NotImplemented |> Error
