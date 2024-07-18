[<RequireQualifiedAccess>]
module Web.Parser.Client.Html

open Web.Parser.Domain.Html
open Infrastructure.Domain.Errors

let load (html: string) =
    try
        let document = Page()
        document.LoadHtml html
        Ok document
    with ex ->
        Error <| NotSupported ex.Message

let getNode (xpath: string) (html: Page) =
    try
        match html.DocumentNode.SelectSingleNode(xpath) with
        | null -> Ok None
        | node -> Ok <| Some node
    with ex ->
        Error <| NotSupported ex.Message

let getNodes (xpath: string) (html: Page) =
    try
        match html.DocumentNode.SelectNodes(xpath) with
        | null -> Ok None
        | nodes -> Ok <| Some nodes
    with ex ->
        Error <| NotSupported ex.Message

let getAttributeValue (attribute: string) (node: Node) =
    try
        match node.GetAttributeValue(attribute, "") with
        | "" -> Ok None
        | value -> Ok <| Some value
    with ex ->
        Error <| NotSupported ex.Message
