using System.Net;

namespace TimetrackerReportingClient.Exceptions;

public class RestApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseContent { get; }

    public RestApiException(HttpStatusCode statusCode, string? responseContent)
        : base($"API request failed [{statusCode}]: {responseContent}")
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public RestApiException(
        HttpStatusCode statusCode,
        string? responseContent,
        string message
    )
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public RestApiException(
        HttpStatusCode statusCode,
        string? responseContent,
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}
