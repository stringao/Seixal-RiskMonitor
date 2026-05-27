using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GeoRisk.API.Domain.Entities;

public class FireStation
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Type { get; set; } = string.Empty; // Voluntarios, Sapadores, Municipais

    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    [MaxLength(100)]
    public string County { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Parish { get; set; }

    public Point Geometry { get; set; } = null!;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(50)]
    public string? OperationalZone { get; set; } // Zona Operacional

    [MaxLength(100)]
    public string? Cim { get; set; } // Comunidade Intermunicipal

    public int PersonnelCount { get; set; }

    public int VehicleCount { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<FireResource> FireResources { get; set; } = new List<FireResource>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
