using static Tests.Steps.Steps;

namespace Tests.UnitTests;

public class ParsingUserInputsToMonth
{
    [Theory]
    [InlineData("January", 1)]
    [InlineData("February", 2)]
    [InlineData("March", 3)]
    [InlineData("April", 4)]
    [InlineData("May", 5)]
    [InlineData("June", 6)]
    [InlineData("July", 7)]
    [InlineData("August", 8)]
    [InlineData("September", 9)]
    [InlineData("October", 10)]
    [InlineData("November", 11)]
    [InlineData("December", 12)]
    [InlineData("january", 1)] // lowercase
    [InlineData("FEBRUARY", 2)] // uppercase
    [InlineData("1", 1)] // numeric string
    [InlineData("2", 2)]
    [InlineData("03", 3)] // zero-padded
    [InlineData("12", 12)]
    public void Should_ParseCorrectly_Given_SupportedMonthFormats(
        string monthInput,
        int expectedMonth
    )
    {
        // Arrange
        var expectedYear = GivenCurrentYear();

        // Act
        var result = WhenMonthIsParsed(monthInput);

        // Assert
        ThenParsedMonthShouldBe(expectedMonth, result.Month);
        ThenCurrentParsedYearShouldBe(expectedYear, result.Year);
    }

    [Theory]
    [InlineData("jan")] // short name lowercase
    [InlineData("Jan")] // short name capitalized
    [InlineData("feb")]
    [InlineData("Feb")]
    [InlineData("mar")]
    [InlineData("dec")]
    [InlineData("Jan1")] // mixed format
    [InlineData("january1")] // mixed format
    [InlineData("13")] // out of range
    [InlineData("0")] // out of range
    [InlineData("-1")] // negative
    [InlineData("Month")] // invalid text
    [InlineData("")] // empty string
    [InlineData("1st")] // ordinal
    public void Should_ThrowException_Given_UnsupportedMonthFormats(string invalidInput)
    {
        // Act & Assert
        ThenParsingShouldFail(invalidInput);
    }
}
