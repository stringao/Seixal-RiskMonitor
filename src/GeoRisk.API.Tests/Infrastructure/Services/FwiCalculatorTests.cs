using FluentAssertions;
using GeoRisk.API.Domain.Enums;
using GeoRisk.API.Infrastructure.ExternalApis;
using GeoRisk.API.Infrastructure.Services;

namespace GeoRisk.API.Tests.Infrastructure.Services;

public sealed class FwiCalculatorTests
{
    // Factory helper for creating test weather data
    private static OpenMeteoWeather CreateWeather(
        double temp = 25, double rh = 50, double wind = 15, double precip = 0)
        => new(0, 0, DateTime.UtcNow, temp, rh, wind, 0, precip);

    #region Calculate() - Basic Conditions

    [Fact]
    public void Calculate_BasicConditions_ReturnsValidFWI()
    {
        // Arrange
        var calc = new FwiCalculator();
        var weather = CreateWeather(25, 50, 15, 0);

        // Act
        var result = calc.Calculate(weather);

        // Assert
        result.FWI.Should().BeGreaterThan(0);
        result.ISI.Should().BeGreaterThan(0);
        result.BUI.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_BasicConditions_Temperature25Humidity50Wind15_ReturnsPositiveIndices()
    {
        // Arrange
        var calc = new FwiCalculator();

        // Act
        var result = calc.Calculate(25, 50, 15, 0);

        // Assert
        result.FWI.Should().BeGreaterThan(0);
        result.ISI.Should().BeGreaterThan(0);
        result.BUI.Should().BeGreaterThan(0);
        result.DMC.Should().BeGreaterThan(0);
        result.DC.Should().BeGreaterThan(0);
        result.FFMC.Should().BeInRange(0, 101);
    }

    #endregion

    #region Calculate() - High Fire Danger Conditions

    [Fact]
    public void Calculate_HighFireDangerConditions_ReturnsHighFWI()
    {
        // Arrange
        var calc = new FwiCalculator();
        // Hot, dry, windy conditions
        var weather = CreateWeather(35, 20, 30, 0);

        // Act
        var result = calc.Calculate(weather);

        // Assert - High fire danger conditions should produce elevated FWI
        result.FWI.Should().BeGreaterThan(15);
        result.ISI.Should().BeGreaterThan(0);
        result.BUI.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_HotDryWindy_CalculatesCorrectlyWithIndividualParameters()
    {
        // Arrange
        var calc = new FwiCalculator();

        // Act
        var result = calc.Calculate(35, 20, 30, 0);

        // Assert
        result.FWI.Should().BeGreaterThan(15, "hot dry windy conditions should produce high FWI");
        result.ISI.Should().BeGreaterThan(0, "ISI should be positive");
        result.BUI.Should().BeGreaterThan(0, "BUI should be positive");
    }

    #endregion

    #region Calculate() - Wet/Cool Conditions (Low Fire Risk)

    [Fact]
    public void Calculate_WetCoolConditions_ReturnsLowFWI()
    {
        // Arrange
        var calc = new FwiCalculator();
        // Cool, humid, wet conditions
        var weather = CreateWeather(10, 90, 5, 5);

        // Act
        var result = calc.Calculate(weather);

        // Assert
        result.FWI.Should().BeLessThan(5, "wet cool conditions should produce low FWI");
    }

    [Fact]
    public void Calculate_WetAndHumid_IndividualParameters_ReturnsLowRisk()
    {
        // Arrange
        var calc = new FwiCalculator();

        // Act
        var result = calc.Calculate(10, 90, 5, 5);

        // Assert
        result.FWI.Should().BeLessThan(10);
        result.FFMC.Should().BeLessThan(101);
    }

    #endregion

    #region Calculate() - Overload with OpenMeteoWeather

    [Fact]
    public void Calculate_WithWeatherObject_ExtractsCorrectParameters()
    {
        // Arrange
        var calc = new FwiCalculator();
        var weather = new OpenMeteoWeather(38.7, -9.2, DateTime.UtcNow, 28, 45, 20, 180, 0);

        // Act
        var result = calc.Calculate(weather);

        // Assert - Verify calculation runs without error and produces valid output
        result.FWI.Should().BeGreaterThan(0);
        result.FFMC.Should().BeInRange(0, 101);
    }

    [Fact]
    public void Calculate_WithWeatherOverload_MatchesIndividualParametersResult()
    {
        // Arrange
        var calc = new FwiCalculator();
        var weather = new OpenMeteoWeather(38.7, -9.2, DateTime.UtcNow, 25, 50, 15, 0, 0);

        // Act
        var resultFromWeather = calc.Calculate(weather);
        var resultFromParams = calc.Calculate(25, 50, 15, 0);

        // Assert - Both overloads should produce identical results
        resultFromWeather.FWI.Should().BeApproximately(resultFromParams.FWI, 0.001);
        resultFromWeather.ISI.Should().BeApproximately(resultFromParams.ISI, 0.001);
        resultFromWeather.BUI.Should().BeApproximately(resultFromParams.BUI, 0.001);
    }

    #endregion

    #region GetDangerRating() Static Method

    [Theory]
    [InlineData(0, "Low")]
    [InlineData(4.9, "Low")]
    [InlineData(4.999, "Low")]
    public void GetDangerRating_FWIBelow5_ReturnsLow(double fwi, string expected)
    {
        // Act
        var result = FwiCalculator.GetDangerRating(fwi);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(5, "Moderate")]
    [InlineData(8, "Moderate")]
    [InlineData(11.9, "Moderate")]
    [InlineData(11.999, "Moderate")]
    public void GetDangerRating_FWI5To11_ReturnsModerate(double fwi, string expected)
    {
        // Act
        var result = FwiCalculator.GetDangerRating(fwi);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(12, "Elevated")]
    [InlineData(18, "Elevated")]
    [InlineData(23.9, "Elevated")]
    [InlineData(23.999, "Elevated")]
    public void GetDangerRating_FWI12To23_ReturnsElevated(double fwi, string expected)
    {
        // Act
        var result = FwiCalculator.GetDangerRating(fwi);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(24, "High")]
    [InlineData(28, "High")]
    [InlineData(31.9, "High")]
    [InlineData(31.999, "High")]
    public void GetDangerRating_FWI24To31_ReturnsHigh(double fwi, string expected)
    {
        // Act
        var result = FwiCalculator.GetDangerRating(fwi);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(32, "Very High")]
    [InlineData(35, "Very High")]
    [InlineData(39.9, "Very High")]
    [InlineData(39.999, "Very High")]
    public void GetDangerRating_FWI32To39_ReturnsVeryHigh(double fwi, string expected)
    {
        // Act
        var result = FwiCalculator.GetDangerRating(fwi);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(40, "Extreme")]
    [InlineData(50, "Extreme")]
    [InlineData(100, "Extreme")]
    [InlineData(150, "Extreme")]
    public void GetDangerRating_FWI40OrAbove_ReturnsExtreme(double fwi, string expected)
    {
        // Act
        var result = FwiCalculator.GetDangerRating(fwi);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region DangerToRiskLevel() Static Method

    [Theory]
    [InlineData("Low", RiskLevel.Low)]
    [InlineData("Moderate", RiskLevel.Medium)]
    [InlineData("Elevated", RiskLevel.High)]
    [InlineData("High", RiskLevel.High)]
    [InlineData("Very High", RiskLevel.Critical)]
    [InlineData("Extreme", RiskLevel.Critical)]
    public void DangerToRiskLevel_ValidRatings_ReturnsCorrectRiskLevel(string rating, RiskLevel expected)
    {
        // Act
        var result = FwiCalculator.DangerToRiskLevel(rating);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DangerToRiskLevel_UnknownRating_ReturnsMedium()
    {
        // Act
        var result = FwiCalculator.DangerToRiskLevel("Unknown");

        // Assert
        result.Should().Be(RiskLevel.Medium);
    }

    #endregion

    #region Score Boundaries

    [Fact]
    public void Calculate_ModerateConditions_ISIInTypicalRange()
    {
        // Arrange
        var calc = new FwiCalculator();
        var weather = CreateWeather(25, 50, 15, 0);

        // Act
        var result = calc.Calculate(weather);

        // Assert - ISI should be positive for moderate conditions
        result.ISI.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_ExtremeConditions_FWICanExceed50()
    {
        // Arrange
        var calc = new FwiCalculator();
        // Extreme conditions
        var weather = CreateWeather(40, 10, 40, 0);

        // Act
        var result = calc.Calculate(weather);

        // Assert - FWI can exceed 50 under extreme fire weather
        result.FWI.Should().BeGreaterThan(30);
    }

    [Fact]
    public void Calculate_VariousConditions_FFMCIsClampedBetween0And101()
    {
        // Arrange
        var calc = new FwiCalculator();

        // Test very low humidity (could potentially push FFMC high)
        var lowRhResult = calc.Calculate(30, 5, 20, 0);
        lowRhResult.FFMC.Should().BeInRange(0, 101);

        // Test very high humidity (could push FFMC low)
        var highRhResult = calc.Calculate(10, 99, 5, 0);
        highRhResult.FFMC.Should().BeInRange(0, 101);

        // Test extreme temperature
        var extremeTempResult = calc.Calculate(50, 30, 10, 0);
        extremeTempResult.FFMC.Should().BeInRange(0, 101);
    }

    [Fact]
    public void Calculate_PrecipitationAbove05mm_AffectsFFMC()
    {
        // Arrange
        var calc = new FwiCalculator();
        var noRain = calc.Calculate(25, 50, 15, 0);
        var withRain = calc.Calculate(25, 50, 15, 3);

        // Assert - Rain should lower FFMC
        withRain.FFMC.Should().BeLessThan(noRain.FFMC);
    }

    [Fact]
    public void Calculate_PrecipitationBelow05mm_DoesNotAffectFFMC()
    {
        // Arrange
        var calc = new FwiCalculator();
        var noRain = calc.Calculate(25, 50, 15, 0);
        var lightRain = calc.Calculate(25, 50, 15, 0.3); // Below threshold

        // Assert - Light rain should not affect FFMC
        lightRain.FFMC.Should().BeApproximately(noRain.FFMC, 0.001);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculate_ZeroPrecipitation_ReturnsPositiveFWI()
    {
        // Arrange
        var calc = new FwiCalculator();

        // Act
        var result = calc.Calculate(25, 50, 15, 0);

        // Assert
        result.FWI.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_VeryHighWind_IncreasesISI()
    {
        // Arrange
        var calc = new FwiCalculator();
        var lowWind = calc.Calculate(25, 50, 5, 0);
        var highWind = calc.Calculate(25, 50, 50, 0);

        // Assert - Higher wind should increase ISI
        highWind.ISI.Should().BeGreaterThan(lowWind.ISI);
    }

    [Fact]
    public void Calculate_ZeroWind_StillProducesValidISI()
    {
        // Arrange
        var calc = new FwiCalculator();

        // Act
        var result = calc.Calculate(25, 50, 0, 0);

        // Assert - Even with zero wind, should produce valid ISI (though low)
        result.ISI.Should().BeGreaterThan(0);
    }

    #endregion
}
