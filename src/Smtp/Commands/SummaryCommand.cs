namespace Smtp.Commands;

using System.CommandLine;
using System.Text.Json;
using Smtp.Configuration;
using Smtp.Services;

public static class SummaryCommand
{
    public static Command Create()
    {
        var serverNameOption = new Option<string?>("--servername")
        {
            Description = "Name of the configured server (optional if only one server configured)"
        };

        var command = new Command("summary", "List unread emails from inbox")
        {
            serverNameOption
        };

        command.SetAction(parseResult =>
        {
            var serverName = parseResult.GetValue(serverNameOption);
            return Execute(serverName);
        });

        return command;
    }

    private static int Execute(string? serverName)
    {
        try
        {
            var configProvider = new ConfigurationProvider();
            var server = configProvider.GetServer(serverName);

            var htmlConverter = new HtmlToTextConverter();
            var emailService = new EmailService(server, htmlConverter);

            var summaries = emailService.GetUnreadEmailsAsync().GetAwaiter().GetResult();

            var json = JsonSerializer.Serialize(
                summaries,
                new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);

            return 0;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("configured") || ex.Message.Contains("Multiple"))
        {
            Console.Error.WriteLine($"Configuration Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }
}
