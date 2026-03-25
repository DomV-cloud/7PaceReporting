using FluentAssertions;
using TimetrackerReportingClient.Exceptions;
using WireMock.Server;
using static Tests.Steps.Steps;

namespace Tests.UnitTests;

public class TimeTrackerClientTests
{
    public class WhenConstructingClient
    {
        [Fact]
        public void Should_ThrowArgumentException_Given_NullBaseUrl()
        {
            // Act
            Action act = () => GivenAConfiguredClient(null!, "valid-token");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_ThrowArgumentException_Given_EmptyBaseUrl()
        {
            // Act
            Action act = () => GivenAConfiguredClient("", "valid-token");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_ThrowArgumentException_Given_NullToken()
        {
            // Act
            Action act = () => GivenAConfiguredClient("https://example.com", null!);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Should_ThrowArgumentException_Given_EmptyToken()
        {
            // Act
            Action act = () => GivenAConfiguredClient("https://example.com", "");

            // Assert
            act.Should().Throw<ArgumentException>();
        }
    }

    public class WhenFetchingWorkLogsForCurrentUser : IDisposable
    {
        private readonly WireMockServer _server;
        private readonly TimetrackerReportingClient.Clients.TimetrackerReportingClient.TimeTrackerClient _client;

        private const string Token = "test-bearer-token";
        private const int Year = 2026;
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
            WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            ThenRequestShouldHaveBearerToken(_server, Token);
        }

        [Fact]
        public void Should_SendRequestWithExpectedQueryParameters_Given_ValidInputs()
        {
            // Arrange
            GivenServerReturnsEmptyWorkLogs(_server);

            // Act
            WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            ThenRequestShouldHaveCorrectQueryParameters(_server, Year, Month);
        }

        [Fact]
        public void Should_ReturnEmptyList_Given_NoWorkLogsInPeriod()
        {
            // Arrange
            GivenServerReturnsEmptyWorkLogs(_server);

            // Act
            var result = WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Should_ReturnWorkLogs_Given_SuccessfulApiResponse()
        {
            // Arrange
            var workLogs = GivenSomeWorkLogs();
            GivenServerReturnsWorkLogs(_server, workLogs);

            // Act
            var result = WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            result.Should().HaveCount(workLogs.Count);
        }

        [Fact]
        public void Should_DeserializeWorkLogsCorrectly_Given_ApiResponseWithData()
        {
            // Arrange
            var workLogs = GivenSomeWorkLogs();
            GivenServerReturnsWorkLogs(_server, workLogs);

            // Act
            var result = WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            ThenWorkLogsShouldMatchExpected(result, workLogs);
        }

        [Fact]
        public void Should_ThrowRestApiException_Given_UnsuccessfulHttpStatusCode()
        {
            // Arrange
            GivenServerReturnsStatusCode(_server, 401);

            // Act
            Action act = () => WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            act.Should().Throw<RestApiException>();
        }

        [Fact]
        public void Should_ThrowRestApiException_Given_ApiErrorInResponseBody()
        {
            // Arrange
            GivenServerReturnsApiError(_server, code: 1001, message: "Unauthorized");

            // Act
            Action act = () => WhenWorkLogsAreFetched(_client, Year, Month);

            // Assert
            act.Should().Throw<RestApiException>().WithMessage("*Unauthorized*");
        }

        public void Dispose() => _server.Dispose();
    }

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
            WhenWorkLogsAreFetched(_client, 2026, 1, allUsers: true);

            // Assert
            ThenRequestShouldTargetAllUsersEndpoint(_server);
        }

        public void Dispose() => _server.Dispose();
    }
}
