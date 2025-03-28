[<RequireQualifiedAccess>]
module Web.Captcha

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open Web.Clients.Domain.Http

[<Literal>]
let ANTI_CAPTCHA_API_KEY = "ANTI_CAPTCHA_API_KEY"

[<Literal>]
let ERROR_CODE = "CaptchaErrorCode"

[<Struct>]
type Task = { TaskId: uint64 }

type Solution = { Text: string }

type TaskResult =
    { Status: string
      Solution: Solution
      ErrorDescription: string option }

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
        match result.ErrorDescription, result.Status with
        | Some error, _ ->
            return
                Error
                <| Operation
                    { Message = $"Captcha API has received the error '{error}'."
                      Code = ERROR_CODE |> Custom |> Some }
        | None, "ready" ->
            return
                match result.Solution.Text with
                | AP.IsInt result -> Ok result
                | _ ->
                    Error
                    <| Operation
                        { Message = $"Captcha API says that '{result.Solution.Text}' is not an integer."
                          Code = ERROR_CODE |> Custom |> Some }
        | _ ->
            do! Async.Sleep 500
            return! tryAgain attempts
    }

let private getTaskResult ct key httpClient task =

    let request, content = createGetTaskResultRequest key task

    let rec innerLoop attempts =
        match attempts with
        | 0 ->
            Error
            <| Operation
                { Message = "No attempts left for the Captcha API task."
                  Code = ERROR_CODE |> Custom |> Some }
            |> async.Return
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
    | 0 -> "Image to solve for Captcha API" |> NotFound |> Error |> async.Return
    | _ ->
        Configuration.getEnvVar ANTI_CAPTCHA_API_KEY
        |> ResultAsync.wrap (fun keyOpt ->
            match keyOpt with
            | None -> ANTI_CAPTCHA_API_KEY |> NotFound |> Error |> async.Return
            | Some key ->
                { Host = "https://api.anti-captcha.com"
                  Headers = None }
                |> Http.Client.init
                |> ResultAsync.wrap (createTask key image ct))
