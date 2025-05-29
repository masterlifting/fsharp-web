[<RequireQualifiedAccess>]
module Web.Clients.Browser.Page

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Microsoft.Playwright
open Web.Clients.Domain.Browser

let load (uri: Uri) (client: Client) =
    async {
        try
            let url = uri |> string
            let! page = client.Browser.NewPageAsync() |> Async.AwaitTask
            match! page.GotoAsync url |> Async.AwaitTask with
            | null -> return $"Page '%s{url}' not found" |> NotFound |> Error
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
                | true -> return Ok page
        with ex ->
            return
                Operation {
                    Message = ex |> Exception.toMessage
                    Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                }
                |> Error
    }

let close (page: Page) =
    async {
        try
            do! page.CloseAsync(PageCloseOptions(RunBeforeUnload = false)) |> Async.AwaitTask
            do! page.Context.CloseAsync() |> Async.AwaitTask
            return Ok()
        with ex ->
            return
                Operation {
                    Message = ex |> Exception.toMessage
                    Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                }
                |> Error
    }

let private tryFindLocator (selector: Selector) (page: Page) =
    let perform () =
        async {
            try
                let locator = page.Locator selector.Value
                let! count = locator.CountAsync() |> Async.AwaitTask
                return
                    match count > 0 with
                    | true -> locator |> Some |> Ok
                    | false -> None |> Ok
            with ex ->
                let error =
                    Operation {
                        Message = "Failed to find locator. " + (ex |> Exception.toMessage)
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
                //TODO: Remove this log when the issue is fixed
                Log.crt
                <| $"Attempt of tryFindLocator for {selector.Value} Error: " + error.Message

                return error |> Error
        }

    Async.retry {
        Delay = 1000
        Attempts = 3u<attempts>
        Perform = perform
    }

module Text =

    let tryFind (selector: Selector) (page: Page) =
        async {
            try
                match! page |> tryFindLocator selector with
                | Error e -> return e |> Error
                | Ok None -> return $"Text selector '{selector.Value}' not found" |> NotFound |> Error
                | Ok(Some locator) ->
                    let! text = locator.InnerTextAsync() |> Async.AwaitTask
                    return
                        match text with
                        | AP.IsString v -> v |> Some |> Ok
                        | _ -> None |> Ok
            with ex ->
                return
                    Operation {
                        Message = ex |> Exception.toMessage
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
                    |> Error
        }

module Input =

    let fill (selector: Selector) (value: string) (page: Page) =
        async {
            try
                match! page |> tryFindLocator selector with
                | Error e -> return e |> Error
                | Ok None -> return $"Input selector '{selector.Value}' not found" |> NotFound |> Error
                | Ok(Some locator) ->
                    do! locator.FillAsync value |> Async.AwaitTask
                    return Ok page
            with ex ->
                return
                    Operation {
                        Message = ex |> Exception.toMessage
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
                    |> Error
        }

module Mouse =

    let private getRandomCoordinates (period: TimeSpan) =
        let count = int (period.TotalMilliseconds / 10.)
        let random = Random()

        // Create a semi-random path with some coherence
        let rec generatePath acc remainingPoints currentX currentY =
            if remainingPoints <= 0 then
                List.rev acc
            else
                // Add some randomness to movement
                let maxStep = 0.5f
                let deltaX = float32 (random.NextDouble() * float maxStep * 2.0 - float maxStep)
                let deltaY = float32 (random.NextDouble() * float maxStep * 2.0 - float maxStep)

                // Sometimes make bigger jumps to simulate quick movements
                let nextX, nextY =
                    if random.NextDouble() < 0.2 then
                        let direction = random.Next(4)
                        match direction with
                        | 0 -> currentX + 1.0f, currentY
                        | 1 -> currentX, currentY + 1.0f
                        | 2 -> currentX - 1.0f, currentY
                        | _ -> currentX, currentY - 1.0f
                    else
                        currentX + deltaX, currentY + deltaY

                // Ensure coordinates are positive or zero
                let nextX = max 0.0f nextX
                let nextY = max 0.0f nextY

                // Round to one decimal place for more natural-looking coordinates
                let roundedX = float32 (Math.Round(float nextX, 1))
                let roundedY = float32 (Math.Round(float nextY, 1))

                generatePath ((roundedX, roundedY) :: acc) (remainingPoints - 1) roundedX roundedY

        // Start at origin and generate a path
        generatePath [] count 0.0f 0.0f

    let click (selector: Selector) (awaiter: Mouse.WaitFor) (page: Page) =
        async {
            try
                match! page |> tryFindLocator selector with
                | Error e -> return e |> Error
                | Ok None -> return $"Mouse selector '{selector.Value}' not found" |> NotFound |> Error
                | Ok(Some locator) ->
                    do! locator.ClickAsync() |> Async.AwaitTask

                    match awaiter with
                    | Mouse.Url pattern ->
                        let! navigation = page.WaitForURLAsync(Regex pattern) |> Async.AwaitTask |> Async.StartChild
                        do! navigation |> Async.Ignore
                        return Ok page
                    | Mouse.Selector selector ->
                        match! page |> tryFindLocator selector with
                        | Error e -> return e |> Error
                        | Ok None -> return $"Mouse selector '{selector.Value}' not found" |> NotFound |> Error
                        | Ok(Some _) -> return Ok page
                    | Mouse.Nothing -> return Ok page
            with ex ->
                return
                    Operation {
                        Message = ex |> Exception.toMessage
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
                    |> Error
        }

    let shuffle (period: TimeSpan) (page: Page) =
        async {
            try
                let coordinates = getRandomCoordinates period

                do!
                    coordinates
                    |> Seq.map (fun (x, y) -> page.Mouse.MoveAsync(x, y))
                    |> Seq.map Async.AwaitTask
                    |> Async.Sequential
                    |> Async.Ignore

                return Ok page
            with ex ->
                return
                    Operation {
                        Message = ex |> Exception.toMessage
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
                    |> Error
        }

module Form =

    let submit (selector: Selector) (urlPattern: Regex) (page: Page) =
        async {
            try
                match! page |> tryFindLocator selector with
                | Error e -> return e |> Error
                | Ok None -> return $"Form selector '{selector.Value}' not found" |> NotFound |> Error
                | Ok(Some locator) ->
                    let! navigation = page.WaitForURLAsync urlPattern |> Async.AwaitTask |> Async.StartChild

                    do! locator.EvaluateAsync "form => form.submit()" |> Async.AwaitTask |> Async.Ignore

                    do! navigation |> Async.Ignore

                    return Ok page
            with ex ->
                return
                    Operation {
                        Message = ex |> Exception.toMessage
                        Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
                    }
                    |> Error
        }
