using GeoRisk.API.Infrastructure.ExternalApis;

namespace GeoRisk.API.Infrastructure.Services;

/// <summary>
/// Canadian Fire Weather Index (FWI) System calculator.
/// Based on the Canadian Forest Fire Weather Index (FWI) System.
/// Reference: https://cwfis.cfs.nrcan.gc.ca/background/summary/fwi
/// </summary>
public static class FwiCalculator
{
    // Month codes for seasonality adjustment (unused)

    /// <summary>
    /// Calculate the complete FWI system values from weather data.
    /// </summary>
    public static FwiResult Calculate(OpenMeteoWeather weather) => Calculate(
        weather.TemperatureCelsius,
        weather.RelativeHumidityPercent,
        weather.WindSpeedKmh,
        weather.PrecipitationMm);

    /// <summary>
    /// Calculate FWI from individual weather parameters.
    /// </summary>
    public static FwiResult Calculate(double tempC, double rhPercent, double windKmh, double precipMm)
    {
        // Fine Fuel Moisture Code (FFMC) - 0 to 101
        double ffmc = CalculateFFMC(tempC, rhPercent, windKmh, precipMm);

        // Duff Moisture Code (DMC) - 0 to ∞
        double dmc = CalculateDMC(tempC, rhPercent, precipMm);

        // Drought Code (DC) - 0 to ∞
        double dc = CalculateDC(tempC, precipMm);

        // Initial Spread Index (ISI) - 0 to ∞
        double isi = CalculateISI(ffmc, windKmh);

        // Build Up Index (BUI) - 0 to ∞
        double bui = CalculateBUI(dmc, dc);

        // Fire Weather Index (FWI) - 0 to ∞
        double fwi = CalculateFWI(isi, bui);

        return new FwiResult(fwi, isi, bui, dmc, dc, ffmc);
    }

    private static double CalculateFFMC(double temp, double rh, double wind, double precip)
    {
        // Normalize rain (precipitation > 0.5mm affects FFMC)
        double rain = precip > 0.5 ? precip : 0;
        double prevFfmc = 85.0; // Assumed previous day's FFMC
        double newFfmc = prevFfmc + 25.6 * (1 - Math.Exp(-0.115 * (rh + 1 - rain))) * (1 - Math.Exp(-0.0685 * wind));

        // Adjust for temperature
        newFfmc -= temp * 0.0136;

        return Math.Max(0, Math.Min(101, newFfmc));
    }

    private static double CalculateDMC(double temp, double rh, double precip)
    {
        double rain = precip > 0.5 ? precip : 0;
        double prevDmc = 6.0;
        double dayLenFactor = 1.0; // Seasonal adjustment

        double newDmc = prevDmc + 100 * rain - 0.5 * (temp - rh) * dayLenFactor;

        return Math.Max(0, newDmc);
    }

    private static double CalculateDC(double temp, double precip)
    {
        double rain = precip > 0.5 ? precip : 0;
        double prevDc = 15.0;

        double adjustedRain = rain > 0 ? Math.Min(rain - 0.5, 3.0) : 0;
        double newDc = prevDc + 0.5 * adjustedRain - 0.1 * temp;

        return Math.Max(0, newDc);
    }

    private static double CalculateISI(double ffmc, double windKmh)
    {
        // ISI = exp((FFMC - 80) / 4.3) * (0.208 * wind^0.5 + 0.208 * wind^(2/3))
        double windMs = windKmh / 3.6;
        double expFactor = Math.Exp((ffmc - 80) / 4.3);
        double isiValue = expFactor * (0.208 * Math.Sqrt(windMs) + 0.208 * Math.Pow(windMs, 2.0 / 3.0));

        return Math.Max(0, isiValue);
    }

    private static double CalculateBUI(double dmc, double dc)
    {
        if (dmc <= 0 && dc <= 0) return 0;

        double buiValue = (dmc + dc) switch
        {
            >= 200 => dmc + dc - 0.778 * (dmc + dc - 80) * Math.Log(dmc + dc - 59.5),
            _ => dmc + dc
        };

        return Math.Max(0, buiValue);
    }

    private static double CalculateFWI(double isi, double bui)
    {
        if (isi <= 0 || bui <= 0) return 0;

        double f = bui <= 80
            ? 0.626 * Math.Pow(isi, 0.659) + 1.114 * Math.Pow(isi, 0.5) * Math.Exp(-bui / 38.3)
            : 0.626 * Math.Pow(isi, 0.659) + 1.114 * Math.Pow(isi, 0.5) * Math.Exp(-bui / 38.3) - 0.0072 * bui;

        return Math.Max(0, f);
    }

    /// <summary>
    /// Get the danger rating based on FWI value.
    /// </summary>
    public static string GetDangerRating(double fwi) => fwi switch
    {
        < 5 => "Low",
        < 12 => "Moderate",
        < 24 => "Elevated",
        < 32 => "High",
        < 40 => "Very High",
        _ => "Extreme"
    };

    /// <summary>
    /// Convert danger rating to risk level for storage.
    /// </summary>
    public static RiskLevel DangerToRiskLevel(string rating) => rating switch
    {
        "Low" => RiskLevel.Low,
        "Moderate" => RiskLevel.Medium,
        "Elevated" => RiskLevel.High,
        "High" => RiskLevel.High,
        "Very High" => RiskLevel.Critical,
        "Extreme" => RiskLevel.Critical,
        _ => RiskLevel.Medium
    };
}

public sealed record FwiResult(
    double FWI,      // Fire Weather Index (main output, 0-100+)
    double ISI,      // Initial Spread Index (rate of spread potential)
    double BUI,      // Build Up Index (fuel availability)
    double DMC,      // Duff Moisture Code
    double DC,       // Drought Code
    double FFMC      // Fine Fuel Moisture Code
);