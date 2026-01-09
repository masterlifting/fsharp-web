module Web.Clients.Domain.BrowserWebApi

type Connection = { BaseUrl: string }

module Dto =

    type Open = { Url: string; Expiration: uint64 } // Expiration in seconds

    type Input = { Selector: string; Value: string }

    type Fill = { Inputs: Input list }

    type Click = { Selector: string }

    type Exists = { Selector: string }

    type Extract = { Selector: string }

    type Execute = {
        Selector: string option
        Function: string
    }
