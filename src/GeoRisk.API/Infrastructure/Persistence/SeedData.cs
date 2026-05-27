using NetTopologySuite.Geometries;

namespace GeoRisk.API.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(GeoRiskDbContext db)
    {
        if (!await db.GeoEvents.AnyAsync())
        {
            var events = new List<GeoEvent>
            {
                MakeEvent(EventType.Fire, "Incendio Florestal - Corroios", "Fogo em zona florestal", -9.155, 38.625, RiskLevel.High, EventSource.ICNF, "ICNF-2026-001", -1),
                MakeEvent(EventType.Fire, "Incendio - Aldeia de Paio Pires", "Fogo em vegetacao seca", -9.085, 38.615, RiskLevel.Critical, EventSource.ANEPC, "ANEPC-2026-001", -2),
                MakeEvent(EventType.Fire, "Queimada - Fogueteiro", "Queimada controlada que escalou", -9.100, 38.640, RiskLevel.Medium, EventSource.ICNF, "ICNF-2026-002", -5),
                MakeEvent(EventType.Flood, "Inundacao - Seixal Centro", "Cheia na zona ribeirinha", -9.103, 38.640, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-002", -3),
                MakeEvent(EventType.Flood, "Alagamento - Amora", "Chuva intensa causou alagamento", -9.115, 38.620, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-001", -10),
                MakeEvent(EventType.Flood, "Transbordo Ribeira - Corroios", "Ribeira transbordou apos chuva", -9.150, 38.630, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-002", -20),
                MakeEvent(EventType.Storm, "Tempestade - Torres da Marinha", "Ventos de 90 km/h", -9.080, 38.635, RiskLevel.Critical, EventSource.IPMA, "IPMA-2026-003", -7),
                MakeEvent(EventType.Storm, "Trovoada - Pinhal General", "Trovoada intensa com granizo", -9.120, 38.655, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-004", -15),
                MakeEvent(EventType.Storm, "Temporal - Seixal", "Chuva forte e vento", -9.100, 38.640, RiskLevel.High, EventSource.IPMA, "IPMA-2026-005", -30),
                MakeEvent(EventType.Landslide, "Deslizamento - Miratejo", "Deslizamento de terra apos chuvas", -9.090, 38.610, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-003", -12),
                MakeEvent(EventType.Landslide, "Erosao - Quinta do Conde", "Erosao costeira accentuada", -9.050, 38.590, RiskLevel.Medium, EventSource.Manual, null, -25),
                MakeEvent(EventType.Industrial, "Fuga de Gas - Paio Pires", "Fuga de gas industrial", -9.085, 38.615, RiskLevel.Critical, EventSource.ANEPC, "ANEPC-2026-004", -4),
                MakeEvent(EventType.Industrial, "Incendio Industrial - Seixal", "Fogo em armazem industrial", -9.105, 38.638, RiskLevel.High, EventSource.ANEPC, "ANEPC-2026-005", -18),
                MakeEvent(EventType.Heatwave, "Onda de Calor - Seixal", "Temperaturas acima de 40C", -9.100, 38.640, RiskLevel.High, EventSource.IPMA, "IPMA-2026-006", -8),
                MakeEvent(EventType.Heatwave, "Alerta Calor - Corroios", "Alerta laranja por calor extremo", -9.150, 38.625, RiskLevel.Medium, EventSource.IPMA, "IPMA-2026-007", -22),
                MakeEvent(EventType.Fire, "Fogo em Mato - Fernao Ferro", "Fogo em mato baixo", -9.130, 38.590, RiskLevel.Low, EventSource.ICNF, "ICNF-2026-003", -14),
                MakeEvent(EventType.Other, "Poluicao - Rio Judeu", "Contaminacao detetada no rio", -9.110, 38.635, RiskLevel.Medium, EventSource.Manual, null, -6),
                MakeEvent(EventType.Storm, "Vento Forte - Paio Pires", "Ramos partidos e telhas levantadas", -9.085, 38.615, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-008", -28),
                MakeEvent(EventType.Flood, "Subida Mare - Seixal", "Mare viva causou inundacao parcial", -9.103, 38.641, RiskLevel.Low, EventSource.IPMA, "IPMA-2026-009", -35),
                MakeEvent(EventType.Industrial, "Derrame - Porto do Seixal", "Derrame de substancia no porto", -9.095, 38.645, RiskLevel.Medium, EventSource.ANEPC, "ANEPC-2026-006", -16)
            };
            db.GeoEvents.AddRange(events);
        }

        if (!await db.FireStations.AnyAsync())
        {
            var fireStations = new List<FireStation>
            {
                MakeFireStation("Bombeiros Voluntarios de Almada", "1508", "Voluntarios", "Rua Candido Capile, 13/14", "2800-043", "Almada", "Setubal", "Almada", "Almada, Cova da Piedade, Pragal e Cacilhas", 38.6808, -9.1586, "212722290", "Area Operacional 1", "Peninsula de Setubal", 45, 12),
                MakeFireStation("Bombeiros Voluntarios de Cacilhas", "1503", "Voluntarios", "Av. Alianca Povo/MFA", "2800-253", "Cacilhas", "Setubal", "Almada", null, 38.6822, -9.1469, "212722520", "Area Operacional 1", "Peninsula de Setubal", 30, 8),
                MakeFireStation("Bombeiros Voluntarios da Trafaria", "1518", "Voluntarios", "Largo dos Marinheiros", "2820-400", "Trafaria", "Setubal", "Almada", "Trafaria", 38.6944, -9.2303, null, "Area Operacional 1", "Peninsula de Setubal", 20, 6),
                MakeFireStation("Associacao Humanitaria de Bombeiros Mistos do Concelho do Seixal", "1505", "Mistos", "Rua dos Lombos - Vale de Gomus", "2840-450", "Seixal", "Setubal", "Seixal", "Seixal, Arrentela e Aldeia de Paio Pires", 38.6382, -9.0984, "212154700", "Area Operacional 1", "Peninsula de Setubal", 85, 22),
                MakeFireStation("Bombeiros Voluntarios da Amora", "1506", "Voluntarios", "Rua das Esfeias - Quinta do Brandelo", "2840-130", "Amora", "Setubal", "Seixal", "Amora", 38.6295, -9.1351, "212276560", "Area Operacional 1", "Peninsula de Setubal", 50, 15),
                MakeFireStation("Bombeiros Voluntarios do Barreiro", "1501", "Voluntarios", "Rua Major/Estacao cp", "2830-316", "Barreiro", "Setubal", "Barreiro", "Barreiro e Lavradio", 38.6653, -9.0603, "212064540", "Area Operacional 2", "Peninsula de Setubal", 40, 12),
                MakeFireStation("Bombeiros Voluntarios de Barreiro Sul e Sueste", "1519", "Voluntarios", "Parque Empresarial do Barreiro - Rua Alexandre Herculano", "2830-148", "Barreiro", "Setubal", "Barreiro", null, 38.6608, -9.0536, "212069690", "Area Operacional 2", "Peninsula de Setubal", 35, 10),
                MakeFireStation("Bombeiros Voluntarios da Moita", "1512", "Voluntarios", "Rua D. Manuel I", "2860-391", "Moita", "Setubal", "Moita", "Moita", 38.6478, -9.0120, "212808170", "Area Operacional 2", "Peninsula de Setubal", 38, 11),
                MakeFireStation("Bombeiros Voluntarios do Montijo", "1513", "Voluntarios", "Avenida dos Bombeiros Voluntarios", "2870-219", "Montijo", "Setubal", "Montijo", "Montijo e Afonsoeiro", 38.7086, -8.9622, "212301542", "Area Operacional 2", "Peninsula de Setubal", 42, 13),
                MakeFireStation("Bombeiros Voluntarios de Alcochete", "1520", "Voluntarios", "Largo do Mercado", "2890-181", "Alcochete", "Setubal", "Alcochete", "Alcochete", 38.7544, -8.9553, "212348210", "Area Operacional 2", "Peninsula de Setubal", 25, 8),
                MakeFireStation("Bombeiros Voluntarios de Canha", "1521", "Voluntarios", "Rua Principal", "2985-090", "Canha", "Setubal", "Montijo", "Canha", 38.4508, -8.7622, "212614210", "Area Operacional 2", "Peninsula de Setubal", 18, 5),
                MakeFireStation("Bombeiros Voluntarios de Palmela", "1514", "Voluntarios", "Largo do Municipal", "2950-051", "Palmela", "Setubal", "Palmela", "Palmela", 38.5644, -8.8958, "212332260", "Area Operacional 2", "Peninsula de Setubal", 35, 10),
                MakeFireStation("Bombeiros Voluntarios do Pinhal Novo", "1515", "Voluntarios", "Rua dos Bombos", "2955-025", "Pinhal Novo", "Setubal", "Palmela", "Pinhal Novo", 38.5536, -8.9031, "212991230", "Area Operacional 2", "Peninsula de Setubal", 30, 9),
                MakeFireStation("Companhia de Bombeiros Sapadores de Setubal", "1502", "Sapadores", "Rua Dr. Antonio Jose de Almeida", "2900-450", "Setubal", "Setubal", "Setubal", "Sao Juliao, Nossa Senhora da Anunciada e Santa Maria da Graca", 38.5367, -8.8668, "265706120", "Area Operacional 2", "Peninsula de Setubal", 60, 18),
                MakeFireStation("Associacao Humanitaria de Bombeiros Voluntarios de Setubal", "1507", "Voluntarios", "Rua Joao de Deus, 1 - Doca dos Pescadores", "2900-412", "Setubal", "Setubal", "Setubal", "Sao Juliao, Nossa Senhora da Anunciada e Santa Maria da Graca", 38.5208, -8.8989, "265538090", "Area Operacional 2", "Peninsula de Setubal", 55, 16),
                MakeFireStation("Bombeiros Voluntarios de Sesimbra", "1516", "Voluntarios", "Rua dos Bombes", "2970-220", "Sesimbra", "Setubal", "Sesimbra", "Santiago", 38.4464, -9.1019, "212229010", "Area Operacional 1", "Peninsula de Setubal", 40, 12),
                MakeFireStation("Bombeiros Voluntarios de Grandola", "1510", "Voluntarios", "Largo do fogo - EN 120", "7570-901", "Grandola", "Setubal", "Grandola", "Grandola", 38.1805, -8.5745, "269439270", "Area Operacional 3", "Peninsula de Setubal", 30, 9),
                MakeFireStation("Bombeiros Mistos de Santiago do Cacem", "1517", "Mistos", "Largo de Sao Joao -EN 261", "7540-135", "Santiago do Cacem", "Setubal", "Santiago do Cacem", "Santiago do Cacem, Santa Cruz e Sao Bartolomeu da Serra", 38.0134, -8.6923, "269701280", "Area Operacional 3", "Alentejo Litoral", 35, 10),
                MakeFireStation("Bombeiros Voluntarios de Sines", "1509", "Voluntarios", "Rua do Iros", "7520-197", "Sines", "Setubal", "Sines", "Sines", 37.9561, -8.8783, "269632470", "Area Operacional 3", "Alentejo Litoral", 32, 9),
                MakeFireStation("Bombeiros Mistos de Alcacer do Sal", "1511", "Mistos", "Largo da Camara Municipal", "7580-134", "Alcacer do Sal", "Setubal", "Alcacer do Sal", "Alcacer do Sal (Santa Maria do Castelo e Santiago) e Santa Susana", 38.3797, -8.5204, "265610300", "Area Operacional 3", "Alentejo Litoral", 28, 8),
                MakeFireStation("Bombeiros Mistos do Torrao", "1522", "Mistos", "Largo do mercado", "2830-300", "Torrao", "Setubal", "Alcacer do Sal", "Torrao", 38.2934, -8.2224, "265940220", "Area Operacional 3", "Alentejo Litoral", 20, 6),
            };
            db.FireStations.AddRange(fireStations);
        }

        await db.SaveChangesAsync();
    }

    private static GeoEvent MakeEvent(EventType type, string title, string desc,
        double lng, double lat, RiskLevel severity, EventSource source,
        string? sourceId, int daysOffset) => new()
    {
        Id = Guid.NewGuid(), EventType = type, Title = title, Description = desc,
        Geometry = new Point(lng, lat) { SRID = 4326 },
        Severity = severity, Source = source, SourceId = sourceId,
        OccurredAt = DateTime.UtcNow.AddDays(daysOffset)
    };

    private static FireStation MakeFireStation(
        string name, string code, string type, string address, string postalCode,
        string city, string district, string county, string? parish,
        double lat, double lng, string? phone, string operationalZone, string cim,
        int personnelCount, int vehicleCount) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Code = code,
        Type = type,
        Address = address,
        PostalCode = postalCode,
        City = city,
        District = district,
        County = county,
        Parish = parish,
        Geometry = new Point(lng, lat) { SRID = 4326 },
        Phone = phone,
        OperationalZone = operationalZone,
        Cim = cim,
        PersonnelCount = personnelCount,
        VehicleCount = vehicleCount,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
