namespace Search;

/// <summary>
/// A configuration or argument problem the user can fix without retrying:
/// unknown engine, ambiguous default, unsupported engine. Maps to exit code 1.
/// </summary>
public class SearchConfigurationException : Exception
{
    public SearchConfigurationException(string message) : base(message) { }
}
