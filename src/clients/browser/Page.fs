[<RequireQualifiedAccess>]
module Web.Clients.Browser.Page

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Browser

let load (uri: Uri) (provider: Provider) =
    try
        async {
            let url = uri |> string
            let! page = provider.Value.NewPageAsync() |> Async.AwaitTask
            match! page.GotoAsync url |> Async.AwaitTask with
            | null -> return $"Page '%s{url}' response not found" |> NotFound |> Error
            | response ->
                match response.Status = 200 with
                | false ->
                    let status = response.Status
                    let statusText = response.StatusText
                    let url = response.Url
                    return
                        $"Page load failed: '%d{status}' '%s{statusText}' '%s{url}'"
                        |> NotSupported
                        |> Error
                | true -> return page |> Page |> Ok
        }
    with ex ->
        Operation {
            Message = ex |> Exception.toMessage
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
        |> Error
        |> async.Return

let waitFor (path: Regex) (page: Page) =
    try
        async {
            do! page.Value.WaitForURLAsync(path) |> Async.AwaitTask
            return page |> Ok
        }
    with ex ->
        Operation {
            Message = ex |> Exception.toMessage
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
        |> Error
        |> async.Return

module Html =

    let tryFindText (selector: string) (page: Page) =
        try
            async {
                let! text = page.Value.Locator(selector).InnerTextAsync() |> Async.AwaitTask
                return
                    match text with
                    | AP.IsString v -> v |> Some |> Ok
                    | _ -> None |> Ok
            }
        with ex ->
            Operation {
                Message = ex |> Exception.toMessage
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
            |> Error
            |> async.Return

module Input =

    let fill (selector: Selector) (value: string) (page: Page) =
        try
            async {
                do! page.Value.FillAsync(selector.Value, value) |> Async.AwaitTask
                return page |> Ok
            }
        with ex ->
            Operation {
                Message = ex |> Exception.toMessage
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
            |> Error
            |> async.Return

module Button =

    let click (selector: Selector) (page: Page) =
        try
            async {
                do! page.Value.ClickAsync(selector.Value) |> Async.AwaitTask
                return page |> Ok
            }
        with ex ->
            Operation {
                Message = ex |> Exception.toMessage
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
            |> Error
            |> async.Return

module Mouse =
    
    let shuffle (page: Page) =
        try
            async {
                do! page.Value.Mouse.MoveAsync(0.f, 0.f) |> Async.AwaitTask
                return page |> Ok
            }
        with ex ->
            Operation {
                Message = ex |> Exception.toMessage
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
            |> Error
            |> async.Return
