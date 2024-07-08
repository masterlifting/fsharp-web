module Web.Http.Captcha

open System
open Infrastructure
open Infrastructure.DSL
open Infrastructure.Domain.Errors
open Web.Domain.Http
open Web.Client.Http
open Infrastructure.DSL.AP
open Infrastructure.DSL.Threading

module AntiCaptcha =
    type Task = { ErrorId: string; TaskId: string }
    type Solution = { Text: string }
    type TaskResult = { Status: string; Solution: Solution }


    let private handleTaskResult ct tryAgain attempts result =
        async {
            match result.Status with
            | "processing" ->
                match ct |> notCanceled with
                | true ->
                    do! Async.Sleep 500
                    return! tryAgain attempts
                | _ -> return Error <| Cancelled "AntiCaptcha"
            | "ready" ->
                return
                    match result.Solution.Text with
                    | IsInt result ->
                        let dateFormat = "yyyy_MM_dd_HH_mm_ss"
                        let time = DateTime.Now.ToString dateFormat
                        let filePath = $"{Environment.CurrentDirectory}/captcha/{time}.txt"
                        IO.File.WriteAllText(filePath, result.ToString())
                        Ok result
                    | _ -> Error <| Parsing $"AntiCaptcha. Is not an integer: '{result.Solution.Text}'."
            | _ ->
                return
                    Error
                    <| Web
                        { Message = "AntiCaptcha. Not supported status."
                          Code = None }
        }

    let private getTaskResult ct key httpClient task =
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

        let rec innerLoop attempts =
            match attempts with
            | 0 -> async { return Error <| Cancelled "AntiCaptcha. No attempts left." }
            | _ ->
                httpClient
                |> Request.Post.waitString ct request content
                |> Response.Json.mapString
                |> ResultAsync.bind' (handleTaskResult ct innerLoop (attempts - 1))

        innerLoop 10

    let private createTask ct key httpClient image =
        let data =
            $@"
            {{
                ""clientKey"": ""%s{key}"",
                ""task"": {{
                    ""type"": ""ImageToTextTask"",
                    ""body"": ""%s{image |> Convert.ToBase64String}"",
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

        httpClient
        |> Request.Post.waitString ct request content
        |> Response.Json.mapString
        |> ResultAsync.bind' (getTaskResult ct key httpClient)

    let solveToInt ct (image: byte array) =
        match image.Length with
        | 0 -> async { return Error <| Configuration "AntiCaptcha. Image is empty." }
        | _ ->
            let dateFormat = "yyyy_MM_dd_HH_mm_ss"
            let time = DateTime.Now.ToString dateFormat
            let filePath = $"{Environment.CurrentDirectory}/captcha/{time}.png"
            IO.File.WriteAllBytes(filePath, image)

            Configuration.getEnvVar "AntiCaptchaApiKey"
            |> ResultAsync.wrap (fun keyOpt ->
                match keyOpt with
                | None -> async { return Error <| Configuration "AntiCaptcha. API Key not found." }
                | Some key ->

                    let createHttpClient url = create url None

                    let createCaptchaTask =
                        ResultAsync.wrap (fun httpClient -> image |> createTask ct key httpClient)

                    createHttpClient "https://api.anti-captcha.com" |> createCaptchaTask)
