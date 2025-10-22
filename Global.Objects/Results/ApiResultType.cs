namespace Global.Objects.Results;

/// <summary>
/// HTTP status codes according to RFC 9110 and IANA registry
/// </summary>
public enum ApiResultType
{
    // 1xx - Informational
    /// <summary>
    /// The server has received the request headers and the client should proceed to send the request body.
    /// </summary>
    Continue = 100,

    /// <summary>
    /// The server agrees to switch protocols as requested by the client.
    /// </summary>
    SwitchingProtocols = 101,

    /// <summary>
    /// The server is processing the request but no response is available yet.
    /// </summary>
    Processing = 102,

    /// <summary>
    /// The server hints about resources that the client might want to pre-fetch.
    /// </summary>
    EarlyHints = 103,

    // 2xx - Success
    /// <summary>
    /// The request succeeded. The response body contains the requested data.
    /// </summary>
    Ok = 200,

    /// <summary>
    /// The request succeeded and a new resource was created. The response includes the location of the resource.
    /// </summary>
    Created = 201,

    /// <summary>
    /// The request was accepted for processing but has not been completed. Processing may occur asynchronously.
    /// </summary>
    Accepted = 202,

    /// <summary>
    /// The returned metadata is not exactly the same as is available from the origin server.
    /// </summary>
    NonAuthoritativeInformation = 203,

    /// <summary>
    /// The request succeeded but there is no content to send in the response body.
    /// </summary>
    NoContent = 204,

    /// <summary>
    /// The client should reset the document view that caused the request.
    /// </summary>
    ResetContent = 205,

    /// <summary>
    /// The server delivers only part of the resource due to a range header sent by the client.
    /// </summary>
    PartialContent = 206,

    /// <summary>
    /// The message body contains multiple status codes for multiple resources.
    /// </summary>
    MultiStatus = 207,

    /// <summary>
    /// The members of a DAV binding have already been enumerated in a preceding response.
    /// </summary>
    AlreadyReported = 208,

    /// <summary>
    /// The server has fulfilled a request for the resource using delta encoding.
    /// </summary>
    ImUsed = 226,

    // 3xx - Redirection
    /// <summary>
    /// The requested resource has multiple representations available.
    /// </summary>
    MultipleChoices = 300,

    /// <summary>
    /// The requested resource has been permanently moved to a new URL.
    /// </summary>
    MovedPermanently = 301,

    /// <summary>
    /// The requested resource temporarily resides under a different URL.
    /// </summary>
    Found = 302,

    /// <summary>
    /// The response to the request can be found under a different URL using GET method.
    /// </summary>
    SeeOther = 303,

    /// <summary>
    /// The resource has not been modified since the last request.
    /// </summary>
    NotModified = 304,

    /// <summary>
    /// Deprecated. The requested resource must be accessed through the proxy given by the Location field.
    /// </summary>
    UseProxy = 305,

    /// <summary>
    /// The requested resource temporarily resides under a different URL.
    /// </summary>
    TemporaryRedirect = 307,

    /// <summary>
    /// The requested resource has been permanently moved to another URL.
    /// </summary>
    PermanentRedirect = 308,

    // 4xx - Client Errors
    /// <summary>
    /// The server cannot process the request due to a client error.
    /// </summary>
    BadRequest = 400,

    /// <summary>
    /// The request requires authentication. The client must include valid credentials.
    /// </summary>
    Unauthorized = 401,

    /// <summary>
    /// Reserved for future use. Originally intended for digital payment systems.
    /// </summary>
    PaymentRequired = 402,

    /// <summary>
    /// The client does not have permission to access the requested resource.
    /// </summary>
    Forbidden = 403,

    /// <summary>
    /// The requested resource could not be found on the server.
    /// </summary>
    NotFound = 404,

    /// <summary>
    /// The request method is not supported for the requested resource.
    /// </summary>
    MethodNotAllowed = 405,

    /// <summary>
    /// The server cannot generate a response matching the list of acceptable values in Accept headers.
    /// </summary>
    NotAcceptable = 406,

    /// <summary>
    /// The client must first authenticate itself with the proxy before proceeding.
    /// </summary>
    ProxyAuthenticationRequired = 407,

    /// <summary>
    /// The server timed out waiting for the request from the client.
    /// </summary>
    RequestTimeout = 408,

    /// <summary>
    /// The request conflicts with the current state of the server.
    /// </summary>
    Conflict = 409,

    /// <summary>
    /// The requested resource is no longer available and will not be available again.
    /// </summary>
    Gone = 410,

    /// <summary>
    /// The server requires a Content-Length header field.
    /// </summary>
    LengthRequired = 411,

    /// <summary>
    /// Preconditions given in request headers failed.
    /// </summary>
    PreconditionFailed = 412,

    /// <summary>
    /// The request entity is larger than limits defined by server.
    /// </summary>
    PayloadTooLarge = 413,

    /// <summary>
    /// The URI requested by the client is too long.
    /// </summary>
    UriTooLong = 414,

    /// <summary>
    /// The server does not support the media type transmitted in the request.
    /// </summary>
    UnsupportedMediaType = 415,

    /// <summary>
    /// The client has asked for a portion of the file but the server cannot supply that portion.
    /// </summary>
    RangeNotSatisfiable = 416,

    /// <summary>
    /// The server cannot meet the requirements of the Expect request-header field.
    /// </summary>
    ExpectationFailed = 417,

    /// <summary>
    /// Any attempt to brew coffee with a teapot should result in this error.
    /// </summary>
    ImATeapot = 418,

    /// <summary>
    /// The request was directed at a server that is not able to produce a response.
    /// </summary>
    MisdirectedRequest = 421,

    /// <summary>
    /// The request was well-formed but was unable to be followed due to semantic errors.
    /// </summary>
    UnprocessableEntity = 422,

    /// <summary>
    /// The resource that is being accessed is locked.
    /// </summary>
    Locked = 423,

    /// <summary>
    /// The request failed due to failure of a previous request.
    /// </summary>
    FailedDependency = 424,

    /// <summary>
    /// The server is unwilling to risk processing a request that might be replayed.
    /// </summary>
    TooEarly = 425,

    /// <summary>
    /// The client should switch to a different protocol.
    /// </summary>
    UpgradeRequired = 426,

    /// <summary>
    /// The origin server requires the request to be conditional.
    /// </summary>
    PreconditionRequired = 428,

    /// <summary>
    /// The user has sent too many requests in a given amount of time.
    /// </summary>
    TooManyRequests = 429,

    /// <summary>
    /// The server is unwilling to process the request because its header fields are too large.
    /// </summary>
    RequestHeaderFieldsTooLarge = 431,

    /// <summary>
    /// The requested resource is unavailable for legal reasons.
    /// </summary>
    UnavailableForLegalReasons = 451,

    // 5xx - Server Errors
    /// <summary>
    /// A generic error message when an unexpected condition was encountered.
    /// </summary>
    InternalServerError = 500,

    /// <summary>
    /// The server does not support the functionality required to fulfill the request.
    /// </summary>
    NotImplemented = 501,

    /// <summary>
    /// The server was acting as a gateway or proxy and received an invalid response from the upstream server.
    /// </summary>
    BadGateway = 502,

    /// <summary>
    /// The server is temporarily unavailable, usually due to high load or maintenance.
    /// </summary>
    ServiceUnavailable = 503,

    /// <summary>
    /// The server was acting as a gateway or proxy and did not receive a timely response from the upstream server.
    /// </summary>
    GatewayTimeout = 504,

    /// <summary>
    /// The server does not support the HTTP protocol version used in the request.
    /// </summary>
    HttpVersionNotSupported = 505,

    /// <summary>
    /// Transparent content negotiation for the request results in a circular reference.
    /// </summary>
    VariantAlsoNegotiates = 506,

    /// <summary>
    /// The server is unable to store the representation needed to complete the request.
    /// </summary>
    InsufficientStorage = 507,

    /// <summary>
    /// The server detected an infinite loop while processing the request.
    /// </summary>
    LoopDetected = 508,

    /// <summary>
    /// Further extensions to the request are required for the server to fulfill it.
    /// </summary>
    NotExtended = 510,

    /// <summary>
    /// The client needs to authenticate to gain network access.
    /// </summary>
    NetworkAuthenticationRequired = 511
}