namespace Smtp.Commands;

using System.CommandLine;
using System.Linq;
using System.Text.Json;
using Smtp.Configuration;
using Smtp.Models;
using Smtp.Services;

public static class MarkReadCommand
{
    public static Command Create()
    {
        var idOption = new Option<string?>("--id")
        {
            Description = "Email unique ID, or comma-separated list of IDs (required)"
        };

        var serverNameOption = new Option<string?>("--servername")
        {
            Description = "Name of the configured server (optional if only one server configured)"
        };

        var command = new Command("mark-read", "Mark one or more emails as read")
        {
            idOption,
            serverNameOption
        };

        command.SetAction(parseResult =>
        {
            var id = parseResult.GetValue(idOption);
            var serverName = parseResult.GetValue(serverNameOption);
            return Execute(id, serverName);
        });

        return command;
    }

    private static int Execute(string? id, string? serverName)
    {
        return ExecuteCore(id, ids =>
        {
            var configProvider = new ConfigurationProvider();
            var server = configProvider.GetServer(serverName);

            var emailService = EmailServiceFactory.Create(server);

            return emailService.MarkEmailsAsReadAsync(ids);
        });
    }

    internal static int ExecuteCore(string? id, Func<List<string>, Task<List<(string Id, bool Success)>>> markAsRead)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.Error.WriteLine("--id is required");
                return 2;
            }

            var ids = ParseIds(id);

            if (ids.Count == 0)
            {
                Console.Error.WriteLine("--id is required");
                return 2;
            }

            var results = markAsRead(ids).GetAwaiter().GetResult();
            var (summary, exitCode) = BuildSummary(results);

            // Compact JSON for single objects (no indentation for token efficiency)
            var json = JsonSerializer.Serialize(summary);
            Console.WriteLine(json);

            return exitCode;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("configured") || ex.Message.Contains("Multiple"))
        {
            Console.Error.WriteLine($"Configuration Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ExceptionFormatting.Chain(ex)}");
            return 2;
        }
    }

    internal static List<string> ParseIds(string id)
    {
        return id.Split(',')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();
    }

    internal static (MarkReadSummary Summary, int ExitCode) BuildSummary(List<(string Id, bool Success)> results)
    {
        var failed = results.Where(r => !r.Success).Select(r => r.Id).ToList();

        var summary = new MarkReadSummary
        {
            Total = results.Count,
            Succeeded = results.Count(r => r.Success),
            Failed = failed
        };

        return (summary, failed.Count == 0 ? 0 : 2);
    }
}
