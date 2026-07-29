namespace Search.Commands;

using System.CommandLine;
using System.Text.Json;
using Agent.Tools.Common;
using Search.Configuration;
using Search.Services;

public static class SearchCommand
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private const int MinResults = 1;
    private const int MaxResults = 50;
    private const int MinTimeout = 1;
    private const int MaxTimeout = 300;

    public static RootCommand Create()
    {
        var queryArgument = new Argument<string>("query")
        {
            Description = "Search terms. Quote the value to include spaces."
        };
        queryArgument.Validators.Add(result =>
        {
            if (string.IsNullOrWhiteSpace(result.GetValueOrDefault<string>()))
            {
                result.AddError("Query must not be empty. Usage: search <query> [--engine <name>]");
            }
        });

        var engineOption = new Option<string?>("--engine")
        {
            Description = "Search engine to use. Defaults to the configured default engine " +
                          $"(supported: {string.Join(", ", SearchEngineFactory.Supported)})."
        };

        var maxResultsOption = new Option<int>("--max-results")
        {
            Description = $"Maximum number of results to return ({MinResults}-{MaxResults}, default 10).",
            DefaultValueFactory = _ => 10,
            CustomParser = result => OptionParsers.BoundedInt(result, "--max-results", MinResults, MaxResults)
        };

        var timeoutOption = new Option<int>("--timeout")
        {
            Description = $"HTTP timeout per request in seconds ({MinTimeout}-{MaxTimeout}, default 30).",
            DefaultValueFactory = _ => 30,
            CustomParser = result => OptionParsers.BoundedInt(result, "--timeout", MinTimeout, MaxTimeout)
        };

        var rootCommand = new RootCommand(
            "Web search CLI tool. Runs a query against a search engine and writes the ranked " +
            "results as JSON on stdout, each with a link and a short summary. The engine is " +
            "selected with --engine; configure the available engines and the default one in " +
            "appsettings.json or agent-tooling.json under the \"Search\" key.")
        {
            queryArgument,
            engineOption,
            maxResultsOption,
            timeoutOption
        };

        rootCommand.SetAction(parseResult => Execute(
            parseResult.GetValue(queryArgument)!,
            parseResult.GetValue(engineOption),
            parseResult.GetValue(maxResultsOption),
            parseResult.GetValue(timeoutOption)));

        return rootCommand;
    }

    private static int Execute(string query, string? engine, int maxResults, int timeout)
    {
        try
        {
            var engineName = new ConfigurationProvider().ResolveEngine(engine);

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeout) };
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

            var searchEngine = SearchEngineFactory.Create(engineName, httpClient);
            var response = searchEngine
                .SearchAsync(query, maxResults, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Console.WriteLine(JsonSerializer.Serialize(response));
            return 0;
        }
        catch (SearchConfigurationException ex)
        {
            Console.Error.WriteLine($"Configuration Error: {ex.Message}");
            return 1;
        }
        catch (SearchRuntimeException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (OperationCanceledException)
        {
            // HttpClient signals its own timeout as a cancellation.
            Console.Error.WriteLine($"Error: the request timed out after {timeout} seconds.");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }
}
