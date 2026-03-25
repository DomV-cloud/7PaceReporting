using WireMock.Server;
using static Tests.Steps.Steps;

namespace Tests.UnitTests;

public class WhenFetchingWorkLogsForAllUsers : IDisposable
{
    private readonly WireMockServer _server;
    private readonly TimetrackerReportingClient.Clients.TimetrackerReportingClient.TimeTrackerClient _client;

    public WhenFetchingWorkLogsForAllUsers()
    {
        _server = GivenARunningApiServer();
        _client = GivenAConfiguredClient(_server.Urls[0], "admin-token");
    }

    [Fact]
    public void Should_UseAllUsersEndpoint_Given_AllUsersFlagIsTrue()
    {
        // Arrange
        GivenServerReturnsEmptyWorkLogs(_server, allUsers: true);

        // Act
        WhenWorkLogsAreFetched(_client, GivenCurrentYear(), 1, allUsers: true);

        // Assert
        ThenRequestShouldTargetAllUsersEndpoint(_server);
    }

    public void Dispose() => _server.Dispose();
}
