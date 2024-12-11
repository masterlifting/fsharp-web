[<RequireQualifiedAccess>]
module Web.Http.Consumer

open System.Threading
open Infrastructure.Domain
open Web.Http.Domain

let start (ct: CancellationToken) (client: Client) =
    async { return Error <| NotSupported "Http.listen." }
