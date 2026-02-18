[<RequireQualifiedAccess>]
module Web.Clients.BrowserWebApi.Request

open System
open System.Text.Json
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open Web.Clients.Http
open Web.Clients.Domain.Http
open Web.Clients.Domain.BrowserWebApi

let private options =
    JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

module Tab =

    /// <summary>
    /// Opens a new browser tab with the specified URL
    /// </summary>
    /// <param name="dto">The open request data containing the URL</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with the tab ID as string</returns>
    let open' (dto: Dto.Open) (ct: CancellationToken) (client: Client) =

        match dto |> Json.serialize' options with
        | Error e -> Error e |> async.Return
        | Ok data ->

            let request = {
                Path = "api/v1/tab/open"
                Headers = None
            }
            let content =
                String {|
                    Data = data
                    Encoding = Text.Encoding.UTF8
                    ContentType = "application/json"
                |}

            client |> Request.post request content ct |> Response.String.readContent ct

    /// <summary>
    /// Returns a PNG screenshot of the specified tab
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with screenshot bytes (PNG)</returns>
    let screenshot (tabId: string) (ct: CancellationToken) (client: Client) =
        let request = {
            Path = $"api/v1/tabs/{tabId}/screenshot"
            Headers = None
        }
        client |> Request.get request ct |> Response.Bytes.readContent ct

    /// <summary>
    /// Closes the specified browser tab
    /// </summary>
    /// <param name="tabId">The ID of the tab to close</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with unit</returns>
    let close (tabId: string) (ct: CancellationToken) (client: Client) =
        let request = {
            Path = $"api/v1/tabs/{tabId}/close"
            Headers = None
        }
        client |> Request.delete request ct |> Response.Unit.read

    /// <summary>
    /// Fills form inputs in the specified tab
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="dto">The fill request data containing the inputs</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with unit</returns>
    let fill (tabId: string) (dto: Dto.Fill) (ct: CancellationToken) (client: Client) =
        match dto |> Json.serialize' options with
        | Error e -> Error e |> async.Return
        | Ok data ->
            let request = {
                Path = $"api/v1/tabs/{tabId}/fill"
                Headers = None
            }
            let content =
                String {|
                    Data = data
                    Encoding = Text.Encoding.UTF8
                    ContentType = "application/json"
                |}
            client |> Request.post request content ct |> Response.Unit.read

    /// <summary>
    /// Applies human-like behavior to the specified tab
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with unit</returns>
    let humanize (tabId: string) (ct: CancellationToken) (client: Client) =
        let request = {
            Path = $"api/v1/tabs/{tabId}/humanize"
            Headers = None
        }
        let content =
            String {|
                Data = ""
                Encoding = Text.Encoding.UTF8
                ContentType = "application/json"
            |}
        client |> Request.post request content ct |> Response.Unit.read

    /// <summary>
    /// Clicks an element in the specified tab and returns the response text (page title if provided)
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="dto">The click request data containing the selector</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with response text</returns>
    let click (tabId: string) (dto: Dto.Click) (ct: CancellationToken) (client: Client) =
        match dto |> Json.serialize' options with
        | Error e -> Error e |> async.Return
        | Ok data ->
            let request = {
                Path = $"api/v1/tabs/{tabId}/click"
                Headers = None
            }
            let content =
                String {|
                    Data = data
                    Encoding = Text.Encoding.UTF8
                    ContentType = "application/json"
                |}
            client |> Request.post request content ct |> Response.String.readContent ct

    /// <summary>
    /// Checks if an element exists in the specified tab
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="dto">The exists request data containing the selector</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with boolean indicating existence</returns>
    let exists (tabId: string) (dto: Dto.Exists) (ct: CancellationToken) (client: Client) =
        match dto |> Json.serialize' options with
        | Error e -> Error e |> async.Return
        | Ok data ->
            let request = {
                Path = $"api/v1/tabs/{tabId}/exists"
                Headers = None
            }
            let content =
                String {|
                    Data = data
                    Encoding = Text.Encoding.UTF8
                    ContentType = "application/json"
                |}
            client
            |> Request.post request content ct
            |> Response.String.readContent ct
            |> ResultAsync.bind (fun res ->
                match res with
                | "true" -> Ok true
                | "false" -> Ok false
                | other ->
                    Error(
                        Operation {
                            Message = $"Unexpected response: {other}"
                            Code = None
                        }
                    ))

    /// <summary>
    /// Extracts text content from an element in the specified tab
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="dto">The extract request data containing the selector</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with the extracted data as string</returns>
    let extract (tabId: string) (dto: Dto.Extract) (ct: CancellationToken) (client: Client) =
        match dto |> Json.serialize' options with
        | Error e -> Error e |> async.Return
        | Ok data ->
            let request = {
                Path = $"api/v1/tabs/{tabId}/extract"
                Headers = None
            }
            let content =
                String {|
                    Data = data
                    Encoding = Text.Encoding.UTF8
                    ContentType = "application/json"
                |}
            client
            |> Request.post request content ct
            |> Response.String.readContent ct
            |> ResultAsync.map (function
                | "" -> None
                | value -> Some value)

    /// <summary>
    /// Executes JavaScript code on a tab or an element in the specified tab
    /// </summary>
    /// <param name="tabId">The ID of the tab</param>
    /// <param name="dto">The execute request data containing the selector and function</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="client">HTTP client</param>
    /// <returns>Async result with response text (e.g. "unit")</returns>
    let execute (tabId: string) (dto: Dto.Execute) (ct: CancellationToken) (client: Client) =
        match dto |> Json.serialize' options with
        | Error e -> Error e |> async.Return
        | Ok data ->
            let request = {
                Path = $"api/v1/tabs/{tabId}/execute"
                Headers = None
            }
            let content =
                String {|
                    Data = data
                    Encoding = Text.Encoding.UTF8
                    ContentType = "application/json"
                |}
            client |> Request.post request content ct |> Response.String.readContent ct
