namespace Search;

/// <summary>
/// A failure while talking to a search engine: HTTP error, rate limiting,
/// bot challenge, or an unparseable response. Maps to exit code 2.
/// </summary>
public class SearchRuntimeException : Exception
{
    public SearchRuntimeException(string message) : base(message) { }

    public SearchRuntimeException(string message, Exception inner) : base(message, inner) { }
}
