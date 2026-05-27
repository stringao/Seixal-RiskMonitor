using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

/// <summary>
/// Stores land use data derived from DGT COS (Carta de Uso e Ocupação do Solo).
/// Critical for fire risk because different land types have different fuel loads.
/// </summary>
public class LandUseDataPoint
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Location of this land use data point.
    /// </summary>
    public Point Location { get; set; } = null!;

    /// <summary>
    /// When this data was fetched from DGT COS.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// COS classification code (e.g., "2.1.1" for artificial forest).
    /// </summary>
    [MaxLength(20)]
    public string? CosCode { get; set; }

    /// <summary>
    /// Human-readable COS description (e.g., "Floresta de produção").
    /// </summary>
    [MaxLength(200)]
    public string? CosDescription { get; set; }

    /// <summary>
    /// Simplified fire risk category derived from COS.
    /// Values: Forest, Agriculture, Shrubland, Urban, Water, Bare
    /// </summary>
    [MaxLength(50)]
    public string? FuelCategory { get; set; }

    /// <summary>
    /// Fuel load index (1-5 scale, 5 = highest fuel load).
    /// </summary>
    public int FuelLoad { get; set; }

    /// <summary>
    /// Fire risk multiplier (0.0 = no risk, 2.0 = extreme risk).
    /// Applied to base fire spread rate.
    /// </summary>
    public double FireRiskMultiplier { get; set; }

    /// <summary>
    /// Dominant species if forest (e.g., "Eucalyptus", "Maritime Pine", "Cork Oak").
    /// </summary>
    [MaxLength(100)]
    public string? DominantSpecies { get; set; }

    /// <summary>
    /// Whether this point is in a Wildland-Urban Interface zone (close to urban areas).
    /// </summary>
    public bool IsWildlandUrbanInterface { get; set; }

    /// <summary>
    /// Grid point identifier for linking with weather/terrain data.
    /// </summary>
    [MaxLength(50)]
    public string? GridPointId { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Fuel category classifications for fire risk assessment.
/// </summary>
public static class FuelCategory
{
    public const string Forest = "Forest";
    public const string Agriculture = "Agriculture";
    public const string Shrubland = "Shrubland";
    public const string Urban = "Urban";
    public const string Water = "Water";
    public const string Bare = "Bare";
}

/// <summary>
/// COS code to fuel category mapping constants.
/// </summary>
public static class CosCodes
{
    // Level 1 COS codes
    public const string Urban = "1";
    public const string Agriculture = "2";
    public const string Forest = "3";
    public const string Shrubland = "4";
    public const string OpenSpaces = "5";
    public const string Water = "6";

    // Level 2 COS codes (agriculture)
    public const string AnnualCrops = "2.1";
    public const string PermanentCrops = "2.2";
    public const string Pastures = "2.3";

    // Level 3 COS codes (forest)
    public const string Eucalyptus = "3.1";
    public const string MaritimePine = "3.2";
    public const string OakCork = "3.3";
}