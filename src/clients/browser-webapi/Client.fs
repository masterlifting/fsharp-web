module Web.Clients.BrowserWebApi.Client

open Web.Clients.Domain.BrowserWebApi
open Web.Clients

let init (connection: Connection) =
    Http.Client.init {
        BaseUrl = connection.BaseUrl + "/api/v1"
        Headers = None
    }
