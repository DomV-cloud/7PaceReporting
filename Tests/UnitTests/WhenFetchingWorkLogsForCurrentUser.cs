using WireMock.Server;
using static Tests.Steps.Steps;

namespace Tests.UnitTests;

public class WhenFetchingWorkLogsForCurrentUser : IDisposable
{
    private readonly WireMockServer _server;
    private readonly TimetrackerReportingClient.Clients.TimetrackerReportingClient.TimeTrackerClient _client;

    private const string Token = "test-bearer-token";
    private const int Month = 1;

    public WhenFetchingWorkLogsForCurrentUser()
    {
        _server = GivenARunningApiServer();
        _client = GivenAConfiguredClient(_server.Urls[0], Token);
    }

    [Fact]
    public void Should_SendRequestWithBearerToken_Given_ValidToken()
    {
        // Arrange
        GivenServerReturnsEmptyWorkLogs(_server);

        // Act
        WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenRequestShouldHaveBearerToken(_server, Token);
    }

    [Fact]
    public void Should_SendRequestWithExpectedQueryParameters_Given_ValidInputs()
    {
        // Arrange
        GivenServerReturnsEmptyWorkLogs(_server);

        // Act
        WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenRequestShouldHaveCorrectQueryParameters(_server, GivenCurrentYear(), Month);
    }

    [Fact]
    public void Should_ReturnEmptyList_Given_NoWorkLogsInPeriod()
    {
        // Arrange
        GivenServerReturnsEmptyWorkLogs(_server);

        // Act
        var result = WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenWorkLogsShouldBeEmpty(result);
    }

    [Fact]
    public void Should_ReturnWorkLogs_Given_SuccessfulApiResponse()
    {
        // Arrange
        var workLogs = GivenSomeWorkLogs();
        GivenServerReturnsWorkLogs(_server, workLogs);

        // Act
        var result = WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenWorkLogsShouldHaveCount(result, workLogs.Count);
    }

    [Fact]
    public void Should_DeserializeWorkLogsCorrectly_Given_ApiResponseWithData()
    {
        // Arrange
        var workLogs = GivenSomeWorkLogs();
        GivenServerReturnsWorkLogs(_server, workLogs);

        // Act
        var result = WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenWorkLogsShouldMatchExpected(result, workLogs);
    }

    [Fact]
    public void Should_ThrowRestApiException_Given_UnsuccessfulHttpStatusCode()
    {
        // Arrange
        GivenServerReturnsStatusCode(_server, 401);

        // Act
        Action act = () => WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenShouldThrowRestApiException(act);
    }

    [Fact]
    public void Should_ThrowRestApiException_Given_ApiErrorInResponseBody()
    {
        // Arrange
        GivenServerReturnsApiError(_server, code: 1001, message: "Unauthorized");

        // Act
        Action act = () => WhenWorkLogsAreFetched(_client, GivenCurrentYear(), Month);

        // Assert
        ThenShouldThrowRestApiExceptionWithMessage(act, "Unauthorized");
    }

    public void Dispose() => _server.Dispose();
}
