[<RequireQualifiedAccess>]
module Web.Clients.Browser.Page

open System
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

module Html =

    let tryFindText (selector: Selector) (page: Page) =
        try
            async {
                let! text = page.Value.Locator(selector.Value).InnerTextAsync() |> Async.AwaitTask
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

    let execute (selector: Selector) (command: string) (page: Page) =
        try
            async {
                let! _ = page.Value.EvalOnSelectorAsync(selector.Value, command) |> Async.AwaitTask
                return page |> Ok
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
                        | 0 -> (currentX + 1.0f, currentY)
                        | 1 -> (currentX, currentY + 1.0f)
                        | 2 -> (currentX - 1.0f, currentY)
                        | _ -> (currentX, currentY - 1.0f)
                    else
                        (currentX + deltaX, currentY + deltaY)

                // Ensure coordinates are positive or zero
                let nextX = max 0.0f nextX
                let nextY = max 0.0f nextY

                // Round to one decimal place for more natural looking coordinates
                let roundedX = float32 (Math.Round(float nextX, 1))
                let roundedY = float32 (Math.Round(float nextY, 1))

                generatePath ((roundedX, roundedY) :: acc) (remainingPoints - 1) roundedX roundedY

        // Start at origin and generate path
        generatePath [] count 0.0f 0.0f

    let shuffle (period: TimeSpan) (page: Page) =
        try
            async {
                let coordinates = getRandomCoordinates period

                do!
                    coordinates
                    |> Seq.map (fun (x, y) -> page.Value.Mouse.MoveAsync(x, y))
                    |> Seq.map Async.AwaitTask
                    |> Async.Sequential
                    |> Async.Ignore

                return page |> Ok
            }
        with ex ->
            Operation {
                Message = ex |> Exception.toMessage
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
            |> Error
            |> async.Return
