[<RequireQualifiedAccess>]
module Web.Clients.Http.Consumer

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain.Http

let start (ct: CancellationToken) (client: Client) =
    async { return Error <| NotSupported "Http.listen." }
