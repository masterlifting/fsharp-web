module Web.Clients.BrowserWebApi.Client

open Web.Clients.Domain.BrowserWebApi
open Web.Clients

let init (connection: Connection) =
    Http.Client.init {
        BaseUrl = connection.BaseUrl
        Headers =
            Map [
                "Accept-Language", [ "en-US,en;q=0.9,ru;q=0.8" ]
                "Connection", [ "keep-alive" ]
                "Sec-Fetch-Dest", [ "document" ]
                "Sec-Fetch-Mode", [ "navigate" ]
                "Sec-Fetch-Site", [ "same-origin" ]
                "Sec-Fetch-User", [ "?1" ]
                "Upgrade-Insecure-Requests", [ "1" ]
                "User-Agent",
                [
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0"
                ]
            ]
            |> Some
    }
