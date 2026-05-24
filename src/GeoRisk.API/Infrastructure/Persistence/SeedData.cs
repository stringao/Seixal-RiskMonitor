using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(GeoRiskDbContext db)
    {
        if (await db.GeoEvents.AnyAsync()) return;
        var events = new List<GeoEvent>
        {
            Make(EventType.Fire, "Incendio Florestal - Corroios", "Fogo em zona florestal", -9.155, 38.625, RiskLevel.High, EventSource.ICNF, "ICNF-2026-001", -1),
            Make(EventType.Fire, "Incendio - Aldeia de Paio Pires", "Fogo em vegetacao seca", -9.085, 38.615, RiskLevel.Critical, EventSource.ANEPC, "ANEPC-2026-001", -2),
            Make(EventType.Fire, "Queimada - Fogueteiro", "Queimada controlada que escalou", -9.100, 38.640, RiskLevel.Medium, EventSource.ICNF, "ICNF-2026-002", -5),
            Make(EventType.Flood, "Inundacao - Seixal Centro", "Cheia na zona ribeirinha", -9.103, 38.640, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-002", -3),
            Make(EventType.Flood, "Alagamento - Amora", "Chuva intensa causou alagamento", -9.115, 38.620, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-001", -10),
            Make(EventType.Flood, "Transbordo Ribeira - Corroios", "Ribeira transbordou apos chuva", -9.150, 38.630, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-002", -20),
            Make(EventType.Storm, "Tempestade - Torres da Marinha", "Ventos de 90 km/h", -9.080, 38.635, RiskLevel.Critical, EventSource.IPMA, "IPMA-2026-003", -7),
            Make(EventType.Storm, "Trovoada - Pinhal General", "Trovoada intensa com granizo", -9.120, 38.655, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-004", -15),
            Make(EventType.Storm, "Temporal - Seixal", "Chuva forte e vento", -9.100, 38.640, RiskLevel.High, EventSource.IPMA, "IPMA-2026-005", -30),
            Make(EventType.Landslide, "Deslizamento - Miratejo", "Deslizamento de terra apos chuvas", -9.090, 38.610, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-003", -12),
            Make(EventType.Landslide, "Erosao - Quinta do Conde", "Erosao costeira acentuada", -9.050, 38.590, RiskLevel.Medium, EventSource.Manual, null, -25),
            Make(EventType.Industrial, "Fuga de Gas - Paio Pires", "Fuga de gas industrial", -9.085, 38.615, RiskLevel.Critical, EventSource.ANEPC, "ANEPC-2026-004", -4),
            Make(EventType.Industrial, "Incendio Industrial - Seixal", "Fogo em armazem industrial", -9.105, 38.638, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-005", -18),
            Make(EventType.Heatwave, "Onda de Calor - Seixal", "Temperaturas acima de 40C", -9.100, 38.640, RiskLevel.High, EventSource.IPMA, "IPMA-2026-006", -8),
            Make(EventType.Heatwave, "Alerta Calor - Corroios", "Alerta laranja por calor extremo", -9.150, 38.625, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-007", -22),
            Make(EventType.Fire, "Fogo em Mato - Fernao Ferro", "Fogo em mato baixo", -9.130, 38.590, RiskLevel.Low, EventSource.ICNF, "ICNF-2026-003", -14),
            Make(EventType.Other, "Poluicao - Rio Judeu", "Contaminacao detetada no rio", -9.110, 38.635, RiskLevel.Medium, EventSource.Manual, null, -6),
            Make(EventType.Storm, "Vento Forte - Paio Pires", "Ramos partidos e telhas levantadas", -9.085, 38.615, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-008", -28),
            Make(EventType.Flood, "Subida Mare - Seixal", "Mare viva causou inundacao parcial", -9.103, 38.641, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-009", -35),
            Make(EventType.Industrial, "Derrame - Porto do Seixal", "Derrame de substancia no porto", -9.095, 38.645, RiskLevel.Medium, EventSource.ANEPC, "ANEPC-2026-006", -16)
        };
        db.GeoEvents.AddRange(events);
        await db.SaveChangesAsync();
    }

    private static GeoEvent Make(EventType type, string title, string desc,
        double lng, double lat, RiskLevel severity, EventSource source,
        string? sourceId, int daysOffset) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Title = title, Description = desc,
        Geometry = new Point(lng, lat) { SRID = 4326 },
        Severity = severity, Source = source, SourceId = sourceId,
        OccurredAt = DateTime.UtcNow.AddDays(daysOffset)
    };
}
