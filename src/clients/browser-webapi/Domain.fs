module Web.Clients.Domain.BrowserWebApi

type Connection = { BaseUrl: string }

module Dto =

    type Open = { Url: string }

    type Input = { Selector: string; Value: string }

    type Fill = { Inputs: Input list }

    type Click = { Selector: string }

    type Exists = { Selector: string }

    type Extract = {
        Selector: string
        Attribute: string option
    }

    type Execute = { Selector: string; Function: string }
