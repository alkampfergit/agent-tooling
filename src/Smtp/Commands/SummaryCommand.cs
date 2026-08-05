namespace Smtp.Commands;

using System.CommandLine;
using System.Text.Json;
using Smtp.Configuration;
using Smtp.Services;
using System.Linq;

public static class SummaryCommand
{
    public static Command Create()
    {
        var serverNameOption = new Option<string?>("--servername")
        {
            Description = "Name of the configured server (optional if only one server configured)"
        };

        var limitOption = new Option<int>("--limit")
        {
            Description = "Maximum number of emails to return (default: 30)"
        };

        var command = new Command("summary", "List unread emails from inbox (sorted by date, newest first)")
        {
            serverNameOption,
            limitOption
        };

        command.SetAction(parseResult =>
        {
            var serverName = parseResult.GetValue(serverNameOption);
            var limit = parseResult.GetValue(limitOption);
            // Default to 30 if not specified
            if (limit == 0) limit = 30;
            return Execute(serverName, limit);
        });

        return command;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    private static int Execute(string? serverName, int limit)
    {
        try
        {
            var configProvider = new ConfigurationProvider();
            var server = configProvider.GetServer(serverName);

            var htmlConverter = new HtmlToTextConverter();
            var emailService = EmailServiceFactory.Create(server, htmlConverter);

            var summaries = emailService.GetUnreadEmailsAsync().GetAwaiter().GetResult();

            // Sort by date (newest first) and apply limit
            var sorted = summaries
                .OrderByDescending(e => e.Date)
                .Take(limit)
                .ToList();

            // CSV format for list responses (more token-efficient for LLMs)
            Console.WriteLine("id,from,subject,date,preview");
            foreach (var email in sorted)
            {
                var preview = EscapeCsv(email.Preview);
                var subject = EscapeCsv(email.Subject);
                var from = EscapeCsv(email.From);
                Console.WriteLine($"{email.Id},{from},{subject},{email.Date:O},{preview}");
            }

            return 0;
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
}
