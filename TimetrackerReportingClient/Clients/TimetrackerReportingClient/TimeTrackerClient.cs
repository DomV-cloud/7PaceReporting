using Newtonsoft.Json;
using RestSharp;
using TimetrackerReportingClient.Constants.Clients.TimetrackerReportingClient;
using TimetrackerReportingClient.Extensions.Clients.RestClient;
using TimetrackerReportingClient.Helpers;
using TimetrackerReportingClient.Models.Api;

namespace TimetrackerReportingClient.Clients.TimetrackerReportingClient;

/// <summary>
/// Client for the 7pace Timetracker REST API v3.x.
///
/// Base URL format:
///   Azure DevOps Services: https://{your-org}.timehub.7pace.com/
///   Azure DevOps Server:   https://{server}/tfs/{collection}/{project}/_apis/timetracker/
///
/// Authentication: Personal Access Token (PAT) or OAuth token from Azure DevOps.
/// </summary>
public class TimeTrackerClient
{
    private readonly RestClient _client;

    public TimeTrackerClient(string baseUrl, string token)
    {
        Ensure.NotNullOrEmpty(baseUrl);
        Ensure.NotNullOrEmpty(token);

        _client = new RestClient(baseUrl.TrimEnd('/'));
        _client.AddDefaultHeader("Authorization", "Bearer " + token);
    }

    /// <summary>
    /// Downloads all work logs for the given month.
    /// Handles pagination automatically (API returns max 500 items per page).
    /// </summary>
    /// <param name="year">Year of the target month.</param>
    /// <param name="month">Month number (1–12).</param>
    /// <param name="allUsers">
    ///   When true, fetches logs for all users (requires Product/Budget/Administrator role).
    ///   When false, fetches only the authenticated user's logs.
    /// </param>
    public List<WorkLog> GetWorkLogsForMonth(int year, int month, bool allUsers = false)
    {
        var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = fromDate.AddMonths(1);

        var endpoint = allUsers ? "api/rest/workLogs/all" : "api/rest/workLogs";
        var allLogs = new List<WorkLog>();
        int skip = 0;

        Console.WriteLine($"Fetching work logs for {fromDate:MMMM yyyy}...");

        while (true)
        {
            var request = new RestRequest(endpoint, Method.Get);
            request.SetupRequest(fromDate, toDate, skip);

            var response = _client.Execute(request);

            Ensure.Successful(response);
            Ensure.NotNullOrEmpty(response.Content!);

            var result = JsonConvert.DeserializeObject<ApiResponse<List<WorkLog>>>(
                response.Content!
            );
            Ensure.NoApiErrors(result!, response);

            if (HasNoData(result!))
                break;

            allLogs.AddRange(result!.Data);
            Console.WriteLine($"Retrieved {allLogs.Count} entries so far...");

            if (IsLastPage(result!))
                break;

            skip += TimetrackerReportingClientConstants.PageSize;
        }

        Console.WriteLine($"Total: {allLogs.Count} work log entries.");
        return allLogs;
    }

    private static bool IsLastPage(ApiResponse<List<WorkLog>> result)
    {
        return result.Data.Count < TimetrackerReportingClientConstants.PageSize;
    }

    private static bool HasNoData(ApiResponse<List<WorkLog>>? result)
    {
        return result?.Data == null || result.Data.Count == 0;
    }
}
