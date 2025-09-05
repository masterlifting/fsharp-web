[<RequireQualifiedAccess>]
module Web.AntiCaptcha

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open Web.Clients.Domain.Http

[<Literal>]
let ERROR_CODE = "CaptchaErrorCode"

[<Struct>]
type Task = { TaskId: uint64 }

let private createTask data ct =
    fun httpClient ->
        let request = { Path = "/createTask"; Headers = None }

        let content =
            String {|
                Data = data
                Encoding = Text.Encoding.UTF8
                ContentType = "application/json"
            |}

        httpClient
        |> Http.Request.post request content ct
        |> Http.Response.String.readContent ct
        |> Http.Response.String.fromJson<Task>

let private getTaskResult<'input, 'output> key task ct =
    fun (httpClient, handleResult: 'input -> Result<'output option, Error'>) ->
        let data =
            $"""
            {{
                "clientKey":"%s{key}",
                "taskId":"%i{task.TaskId}"
            }}
            """

        let request = {
            Path = "/getTaskResult"
            Headers = None
        }

        let content =
            String {|
                Data = data
                Encoding = Text.Encoding.UTF8
                ContentType = "application/json"
            |}

        let rec innerLoop attempts =
            match attempts with
            | 0 ->
                Error
                <| Operation {
                    Message = "No attempts left for the Captcha API task."
                    Code = ERROR_CODE |> Custom |> Some
                }
                |> async.Return
            | _ ->
                if ct |> canceled then
                    innerLoop 0
                else
                    httpClient
                    |> Http.Request.post request content ct
                    |> Http.Response.String.readContent ct
                    |> Http.Response.String.fromJson<'input>
                    |> ResultAsync.bind handleResult
                    |> ResultAsync.bindAsync (function
                        | Some result -> result |> Ok |> async.Return
                        | None ->
                            async {
                                do! Async.Sleep 500
                                return! innerLoop (attempts - 1)
                            })

        innerLoop 5

let private init =
    fun createTask ->
        {
            BaseUrl = "https://api.anti-captcha.com"
            Headers = None
        }
        |> Http.Client.init
        |> ResultAsync.wrap createTask

module Number =

    type Solution = { Text: string }

    type TaskResult = {
        Status: string
        Solution: Solution
        ErrorDescription: string option
    }

    let private createTaskModel key image =
        $"""
        {{
            "clientKey": "%s{key}",
            "task": {{
                "type": "ImageToTextTask",
                "body": "%s{image |> Convert.ToBase64String}",
                "phrase": false,
                "case": false,
                "numeric": 0,
                "math": 0,
                "minLength": 0,
                "maxLength": 0
            }}
        }}
        """

    let private handleTaskResult result =
        match result.ErrorDescription, result.Status with
        | Some error, _ ->
            Error
            <| Operation {
                Message = $"Captcha API has received the error '{error}'."
                Code = ERROR_CODE |> Custom |> Some
            }
        | None, "ready" ->
            match result.Solution.Text with
            | AP.IsInt result -> Ok(Some result)
            | _ ->
                Error
                <| Operation {
                    Message = $"Captcha API says that '{result.Solution.Text}' is not an integer."
                    Code = ERROR_CODE |> Custom |> Some
                }
        | _ -> Ok None

    let fromImage ct apiKey (image: byte array) =
        init (fun httpClient ->
            let model = createTaskModel apiKey image

            httpClient
            |> createTask model ct
            |> ResultAsync.bindAsync (fun task ->
                (httpClient, handleTaskResult) |> getTaskResult<TaskResult, int> apiKey task ct))

module ReCaptcha =

    module V3 =
        
        module Enterprise =
            type TaskResult = {
                Success: bool
                ChallengeTs: DateTime
                Hostname: string
                Action: string
                Score: float
                ErrorCodes: string option
            }

            let private createTaskModel apiKey siteUri siteKey =
                $"""
                {{
                    "clientKey": "%s{apiKey}",
                    "task": {{
                        "type": "ReCaptchaV3TaskProxyless",
                        "websiteURL": "%s{siteUri |> string}",
                        "websiteKey": "%s{siteKey}",
                        "action": "login_or_register",
                        "isEnterprise": true
                    }}
                }}
                """

            let private handleTaskResult result =
                match result.ErrorCodes, result.Success with
                | Some error, _ ->
                    Error
                    <| Operation {
                        Message = $"Captcha API has received the error '{error}'."
                        Code = ERROR_CODE |> Custom |> Some
                    }
                | None, true -> Ok(Some(result.Score |> string))
                | None, false ->
                    Error
                    <| Operation {
                        Message = "Captcha API says that the task is not solved."
                        Code = ERROR_CODE |> Custom |> Some
                    }

            let fromPage ct apiKey (siteUri: Uri) (siteKey: string) =
                init (fun httpClient ->
                    let model = createTaskModel apiKey siteUri siteKey
                    httpClient
                    |> createTask model ct
                    |> ResultAsync.bindAsync (fun task ->
                        (httpClient, handleTaskResult)
                        |> getTaskResult<TaskResult, string> apiKey task ct))
