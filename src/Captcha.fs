module Web.Captcha

open System
open Infrastructure
open Web.Http.Domain

[<Literal>]
let CaptchaErrorCode = "CaptchaErrorCode"

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
                          Code = Some CaptchaErrorCode }
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
                |> Http.Client.Request.post ct request content
                |> Http.Client.Response.String.readContent ct
                |> Http.Client.Response.String.fromJson<TaskResult>
                |> ResultAsync.bind' (handleTaskResult innerLoop (attempts - 1))

    innerLoop 5

let private createTask ct key httpClient image =
    let request, content = createCreateTaskRequest key image

    httpClient
    |> Http.Client.Request.post ct request content
    |> Http.Client.Response.String.readContent ct
    |> Http.Client.Response.String.fromJson<Task>
    |> ResultAsync.bind' (getTaskResult ct key httpClient)

let solveToInt ct (image: byte array) =
    match image.Length with
    | 0 -> async { return Error <| NotFound "Captcha. Image to solve." }
    | _ ->
        Configuration.getEnvVar "ANTI_CAPTCHA_API_KEY"
        |> ResultAsync.wrap (fun keyOpt ->
            match keyOpt with
            | None -> async { return Error <| NotFound "AntiCaptcha. API Key." }
            | Some key ->

                let createHttpClient url = Http.Client.create url None

                let createCaptchaTask =
                    ResultAsync.wrap (fun httpClient -> image |> createTask ct key httpClient)

                createHttpClient "https://api.anti-captcha.com" |> createCaptchaTask)
