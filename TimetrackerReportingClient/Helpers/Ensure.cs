using System.Runtime.CompilerServices;
using RestSharp;
using TimetrackerReportingClient.Exceptions;
using TimetrackerReportingClient.Models.Api;

namespace TimetrackerReportingClient.Helpers;

public static class Ensure
{
    public static void NotNull<T>(T obj, string paramName)
        where T : class
    {
        if (obj == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void NotNullOrEmpty(
        string parameter,
        [CallerArgumentExpression(nameof(parameter))] string? paramName = null
    )
    {
        if (string.IsNullOrEmpty(parameter))
        {
            throw new ArgumentException("String cannot be null or empty.", paramName);
        }
    }

    public static void Successful(RestResponse response)
    {
        Ensure.NotNull(response, nameof(response));
        if (!response.IsSuccessful)
        {
            throw new RestApiException(response.StatusCode, response.Content);
        }
    }

    public static void NoApiErrors<T>(ApiResponse<T> result, RestResponse response)
        where T : class
    {
        Ensure.NotNull(result, nameof(result));
        if (result.Error != null)
        {
            throw new RestApiException(
                response.StatusCode,
                response.Content,
                $"API error [{result.Error.Code}]: {result.Error.Message}"
            );
        }
    }

    public static void NotEmpty<T>(ICollection<T> list, string message)
    {
        Ensure.NotNull(list, nameof(list));
        if (list.Count == 0)
        {
            throw new InvalidOperationException(message);
        }
    }
}
