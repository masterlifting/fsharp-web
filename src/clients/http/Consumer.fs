[<RequireQualifiedAccess>]
module Web.Clients.Http.Consumer

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain.Http

let start (_: CancellationToken) (_: Client) =
    async { return Error <| NotSupported "Http consumer" }
