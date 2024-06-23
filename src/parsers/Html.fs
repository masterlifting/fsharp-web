module Web.Parser.Html
open Infrastructure.Domain.Errors
open HtmlAgilityPack

let load (html: string) =
    try
        let document = HtmlDocument()
        document.LoadHtml html
        Ok document
    with ex ->
        Error <| ParsingError ex.Message
        

