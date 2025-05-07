[<RequireQualifiedAccess>]
module Web.Clients.Browser.Html

open HtmlAgilityPack
open Infrastructure.Domain
open Infrastructure.Prelude

let load (html: string) =
    try
        let document = HtmlDocument()
        document.LoadHtml html
        Ok document
    with ex ->
        Error
        <| Operation {
            Message = "Failed to load HTML document. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }

let getNode (xpath: string) (html: HtmlDocument) =
    try
        match html.DocumentNode.SelectSingleNode(xpath) with
        | null -> Ok None
        | node -> Ok <| Some node
    with ex ->
        Error
        <| Operation {
            Message = "Failed to select node. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }

let getNodes (xpath: string) (html: HtmlDocument) =
    try
        match html.DocumentNode.SelectNodes(xpath) with
        | null -> Ok None
        | nodes -> Ok <| Some nodes
    with ex ->
        Error
        <| Operation {
            Message = "Failed to select nodes. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }

let getAttributeValue (attribute: string) (node: HtmlNode) =
    try
        match node.GetAttributeValue(attribute, "") with
        | "" -> Ok None
        | value -> Ok <| Some value
    with ex ->
        Error
        <| Operation {
            Message = "Failed to get attribute value. " + (ex |> Exception.toMessage)
            Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
        }
