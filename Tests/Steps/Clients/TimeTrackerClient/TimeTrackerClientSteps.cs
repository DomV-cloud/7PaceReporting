using FluentAssertions;
using Newtonsoft.Json;
using TimetrackerReportingClient.Clients.TimetrackerReportingClient;
using TimetrackerReportingClient.Exceptions;
using TimetrackerReportingClient.Models.Api;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tests.Steps;

public static partial class Steps
{
    #region Given (TimeTrackerClient)

    public static WireMockServer GivenARunningApiServer() => WireMockServer.Start();

    public static TimeTrackerClient GivenAConfiguredClient(string baseUrl, string token) =>
        new(baseUrl, token);

    public static List<WorkLog> GivenSomeWorkLogs() =>
        [
            new WorkLog
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Length = 3600,
                Timestamp = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc),
                Comment = "Feature work",
                Billable = true,
                WorkItemId = 42,
                UserId = new Guid("22222222-2222-2222-2222-222222222222"),
            },
            new WorkLog
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Length = 7200,
                Timestamp = new DateTime(2026, 1, 16, 10, 0, 0, DateTimeKind.Utc),
                Comment = "Bug fix",
                Billable = false,
                WorkItemId = 99,
                UserId = new Guid("44444444-4444-4444-4444-444444444444"),
            },
        ];

    public static void GivenServerReturnsWorkLogs(
        WireMockServer server,
        List<WorkLog> workLogs,
        bool allUsers = false
    )
    {
        var path = allUsers ? "/api/rest/workLogs/all" : "/api/rest/workLogs";
        var body = JsonConvert.SerializeObject(new ApiResponse<List<WorkLog>> { Data = workLogs });

        server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(body)
            );
    }

    public static void GivenServerReturnsEmptyWorkLogs(
        WireMockServer server,
        bool allUsers = false
    ) => GivenServerReturnsWorkLogs(server, [], allUsers);

    public static void GivenServerReturnsStatusCode(WireMockServer server, int statusCode)
    {
        server
            .Given(Request.Create().WithPath("/api/rest/workLogs").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode));
    }

    public static void GivenServerReturnsApiError(WireMockServer server, int code, string message)
    {
        var body = JsonConvert.SerializeObject(
            new ApiResponse<List<WorkLog>>
            {
                Error = new ApiError { Code = code, Message = message },
            }
        );

        server
            .Given(Request.Create().WithPath("/api/rest/workLogs").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(body)
            );
    }

    #endregion

    #region When (TimeTrackerClient)

    public static List<WorkLog> WhenWorkLogsAreFetched(
        TimeTrackerClient client,
        int year,
        int month,
        bool allUsers = false
    ) => client.GetWorkLogsForMonth(year, month, allUsers);

    #endregion

    #region Then (TimeTrackerClient)

    public static void ThenRequestShouldHaveBearerToken(WireMockServer server, string token)
    {
        server
            .LogEntries.Should()
            .Contain(entry =>
                entry.RequestMessage.Headers!.ContainsKey("Authorization")
                && entry.RequestMessage.Headers["Authorization"].Any(v => v == $"Bearer {token}")
            );
    }

    public static void ThenRequestShouldHaveCorrectQueryParameters(
        WireMockServer server,
        int year,
        int month
    )
    {
        var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = fromDate.AddMonths(1);

        var entry = server.LogEntries[0];
        var decodedUrl = Uri.UnescapeDataString(entry.RequestMessage.AbsoluteUrl);

        decodedUrl.Should().Contain($"$fromTimestamp={fromDate.ToString("o")}");
        decodedUrl.Should().Contain($"$toTimestamp={toDate.ToString("o")}");
        decodedUrl.Should().Contain("$count=500");
        decodedUrl.Should().Contain("$skip=0");
        decodedUrl.Should().Contain("api-version=3.2");
    }

    public static void ThenConstructingClientShouldThrowArgumentException(
        string baseUrl,
        string token
    )
    {
        Action act = () => new TimeTrackerClient(baseUrl, token);
        act.Should().Throw<ArgumentException>();
    }

    public static void ThenRequestShouldTargetAllUsersEndpoint(WireMockServer server)
    {
        server
            .LogEntries.Should()
            .Contain(entry => entry.RequestMessage.Path == "/api/rest/workLogs/all");
    }

    public static void ThenWorkLogsShouldBeEmpty(List<WorkLog> result)
    {
        result.Should().BeEmpty();
    }

    public static void ThenWorkLogsShouldHaveCount(List<WorkLog> result, int expectedCount)
    {
        result.Should().HaveCount(expectedCount);
    }

    public static void ThenShouldThrowRestApiException(Action act)
    {
        act.Should().Throw<RestApiException>();
    }

    public static void ThenShouldThrowRestApiExceptionWithMessage(
        Action act,
        string expectedMessage
    )
    {
        act.Should().Throw<RestApiException>().WithMessage($"*{expectedMessage}*");
    }

    public static void ThenWorkLogsShouldMatchExpected(List<WorkLog> actual, List<WorkLog> expected)
    {
        actual
            .Should()
            .BeEquivalentTo(
                expected,
                options =>
                    options
                        .Including(x => x.Id)
                        .Including(x => x.Length)
                        .Including(x => x.Timestamp)
                        .Including(x => x.Comment)
                        .Including(x => x.Billable)
                        .Including(x => x.WorkItemId)
                        .Including(x => x.UserId)
            );
    }

    #endregion
}
