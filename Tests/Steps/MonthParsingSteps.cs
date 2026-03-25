using FluentAssertions;
using TimetrackerReportingClient.Extensions;

namespace Tests.Steps;

public static partial class Steps
{
    #region Given
    public static int GivenCurrentYear()
    {
        return DateTime.Now.Year;
    }
    #endregion

    #region When
    public static (int Year, int Month) WhenMonthIsParsed(string monthToParse)
    {
        var parsedMonth = monthToParse.ParseToMonth();
        return parsedMonth;
    }
    #endregion

    #region Then
    public static void ThenParsedMonthShouldBe(int expectedMonth, int actualMonth)
    {
        actualMonth.Should().Be(expectedMonth);
    }

    public static void ThenCurrentParsedYearShouldBe(int expectedYear, int actualYear)
    {
        actualYear.Should().Be(expectedYear);
    }

    public static void ThenParsingShouldFail(string monthToParse)
    {
        Action act = () => monthToParse.ParseToMonth();
        act.Should().Throw<ArgumentException>();
    }
    #endregion
}
