namespace GeoRisk.API.Infrastructure.AI;

public interface ILlmProvider
{
    Task<string> CompleteAsync(string system, string user, CancellationToken ct = default);
    Task<T> CompleteStructuredAsync<T>(string system, string user, CancellationToken ct = default) where T : class;
}
