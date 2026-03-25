using RestSharp;
using TimetrackerReportingClient.Constants.Clients.TimetrackerReportingClient;
using TimetrackerReportingClient.Exceptions;
using TimetrackerReportingClient.Models.Api;

namespace TimetrackerReportingClient.Extensions.Clients.RestClient;

public static class RestClientExtensions
{
    public static RestRequest SetupRequest(
        this RestRequest request,
        DateTime fromDate,
        DateTime toDate,
        int skip
    )
    {
        request.AddQueryParameter("$fromTimestamp", fromDate.ToString("o"));
        request.AddQueryParameter("$toTimestamp", toDate.ToString("o"));
        request.AddQueryParameter(
            "$count",
            TimetrackerReportingClientConstants.PageSize.ToString()
        );
        request.AddQueryParameter("$skip", skip.ToString());
        request.AddQueryParameter("api-version", TimetrackerReportingClientConstants.ApiVersion);

        return request;
    }
}
