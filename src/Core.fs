module Web.Core

module Http =
    let get (url: string) =
        async { return Error "Http.get not implemented." }

    let post (url: string) (data: byte[]) =
        async { return Error "Http.post not implemented." }
