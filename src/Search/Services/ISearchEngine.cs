namespace Search.Services;

using Search.Models;

/// <summary>
/// A web search backend. Implementations are selected by name via
/// <see cref="SearchEngineFactory"/>.
/// </summary>
public interface ISearchEngine
{
    Task<SearchResponse> SearchAsync(string query, int maxResults, CancellationToken cancellationToken);
}
