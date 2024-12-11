[<RequireQualifiedAccess>]
module Web.Captcha

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Http.Domain

[<Literal>]
let ErrorCode = "CaptchaErrorCode"

[<Struct>]
type Task = { TaskId: uint64 }

type Solution = { Text: string }
type TaskResult = { Status: string; Solution: Solution }


let private createGetTaskResultRequest key task =
    let data =
        $@"
            {{
                ""clientKey"":""{key}"",
                ""taskId"":""{task.TaskId}""
            }}"

    let request =
        { Path = "/getTaskResult"
          Headers = None }

    let content =
        String
            {| Data = data
               Encoding = Text.Encoding.UTF8
               MediaType = "application/json" |}

    request, content

let private createCreateTaskRequest key image =
    let data =
        $@"
            {{
                ""clientKey"": ""{key}"",
                ""task"": {{
                    ""type"": ""ImageToTextTask"",
                    ""body"": ""{image |> Convert.ToBase64String}"",
                    ""phrase"": false,
                    ""case"": false,
                    ""numeric"": 0,
                    ""math"": 0,
                    ""minLength"": 0,
                    ""maxLength"": 0
                }}
            }}"

    let request = { Path = "/createTask"; Headers = None }

    let content =
        String
            {| Data = data
               Encoding = Text.Encoding.UTF8
               MediaType = "application/json" |}

    request, content


let private handleTaskResult tryAgain attempts result =
    async {
        match result.Status with
        | "ready" ->
            return
                match result.Solution.Text with
                | AP.IsInt result -> Ok result
                | _ ->
                    Error
                    <| Operation
                        { Message = $"Captcha. The '{result.Solution.Text}' is not an integer."
                          Code = ErrorCode |> Custom |> Some }
        | _ ->
            do! Async.Sleep 500
            return! tryAgain attempts
    }

let private getTaskResult ct key httpClient task =

    let request, content = createGetTaskResultRequest key task

    let rec innerLoop attempts =
        match attempts with
        | 0 -> async { return Error <| Canceled "Captcha. No attempts left." }
        | _ ->
            if ct |> canceled then
                innerLoop 0
            else
                httpClient
                |> Http.Request.post request content ct
                |> Http.Response.String.readContent ct
                |> Http.Response.String.fromJson<TaskResult>
                |> ResultAsync.bindAsync (handleTaskResult innerLoop (attempts - 1))

    innerLoop 5

let private createTask key image ct httpClient =
    let request, content = createCreateTaskRequest key image

    httpClient
    |> Http.Request.post request content ct
    |> Http.Response.String.readContent ct
    |> Http.Response.String.fromJson<Task>
    |> ResultAsync.bindAsync (getTaskResult ct key httpClient)

let solveToInt ct (image: byte array) =
    match image.Length with
    | 0 -> "Captcha. Image to solve." |> NotFound |> Error |> async.Return
    | _ ->
        Configuration.getEnvVar "ANTI_CAPTCHA_API_KEY"
        |> ResultAsync.wrap (fun keyOpt ->
            match keyOpt with
            | None -> "ANTI_CAPTCHA_API_KEY" |> NotFound |> Error |> async.Return
            | Some key ->
                { BaseUrl = "https://api.anti-captcha.com"
                  Headers = None }
                |> Http.Client.init
                |> ResultAsync.wrap (createTask key image ct))
