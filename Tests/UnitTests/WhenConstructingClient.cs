using static Tests.Steps.Steps;

namespace Tests.UnitTests;

public class WhenConstructingClient
{
    [Fact]
    public void Should_ThrowArgumentException_Given_NullBaseUrl()
    {
        // Act & Assert
        ThenConstructingClientShouldThrowArgumentException(null!, "valid-token");
    }

    [Fact]
    public void Should_ThrowArgumentException_Given_EmptyBaseUrl()
    {
        // Act & Assert
        ThenConstructingClientShouldThrowArgumentException("", "valid-token");
    }

    [Fact]
    public void Should_ThrowArgumentException_Given_NullToken()
    {
        // Act & Assert
        ThenConstructingClientShouldThrowArgumentException("https://example.com", null!);
    }

    [Fact]
    public void Should_ThrowArgumentException_Given_EmptyToken()
    {
        // Act & Assert
        ThenConstructingClientShouldThrowArgumentException("https://example.com", "");
    }
}
