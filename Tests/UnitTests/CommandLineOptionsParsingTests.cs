using FluentAssertions;
using TimetrackerReportingClient.Extensions;
using TimetrackerReportingClient.Models.CommandLine;

namespace Tests.UnitTests;

public class CommandLineOptionsParsingTests
{
    public class WhenParsingTokenArgument
    {
        [Fact]
        public void Should_SetToken_Given_ValidTokenProvided()
        {
            // Arrange
            var args = new[] { "-t", "my-token" };

            // Act
            var result = ((CommandLineOptions)null!).InitCLI(args);

            // Assert
            result.Token.Should().Be("my-token");
        }
    }

    public class WhenParsingMonthAndYearArguments
    {
        [Fact]
        public void Should_SetMonthAndYear_Given_ValidValuesProvided()
        {
            // Arrange
            var args = new[] { "-m", "3", "-y", "2026" };

            // Act
            var result = ((CommandLineOptions)null!).InitCLI(args);

            // Assert
            result.Month.Should().Be(3);
            result.Year.Should().Be(2026);
        }
    }

    public class WhenNoArgumentsProvided
    {
        [Fact]
        public void Should_ReturnDefaultValues_Given_EmptyArguments()
        {
            // Arrange
            var args = Array.Empty<string>();

            // Act
            var result = ((CommandLineOptions)null!).InitCLI(args);

            // Assert
            result.Token.Should().BeNull();
            result.Month.Should().Be(0);
        }
    }
}
