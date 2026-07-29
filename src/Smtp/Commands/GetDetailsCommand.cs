namespace Smtp.Commands;

using System.CommandLine;
using System.Text.Json;
using Smtp.Configuration;
using Smtp.Services;

public static class GetDetailsCommand
{
    public static Command Create()
    {
        var idOption = new Option<string?>("--id")
        {
            Description = "Email unique ID from summary command (required)"
        };

        var serverNameOption = new Option<string?>("--servername")
        {
            Description = "Name of the configured server (optional if only one server configured)"
        };

        var command = new Command("get-details", "Retrieve full email details by ID with HTML-to-text conversion")
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
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.Error.WriteLine("--id is required");
                return 2;
            }

            var configProvider = new ConfigurationProvider();
            var server = configProvider.GetServer(serverName);

            var htmlConverter = new HtmlToTextConverter();
            var emailService = new EmailService(server, htmlConverter);

            var details = emailService.GetEmailDetailsAsync(id).GetAwaiter().GetResult();
            if (details == null)
            {
                Console.Error.WriteLine($"Email ID {id} not found");
                return 2;
            }

            var json = JsonSerializer.Serialize(
                details,
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
