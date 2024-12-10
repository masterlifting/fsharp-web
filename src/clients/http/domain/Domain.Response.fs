module Web.Http.Domain.Response

type Response<'a> =
    { Content: 'a
      StatusCode: int
      Headers: Headers }