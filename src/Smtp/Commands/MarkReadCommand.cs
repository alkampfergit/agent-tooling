namespace Smtp.Commands;

using System.CommandLine;
using System.Text.Json;
using Smtp.Configuration;
using Smtp.Models;
using Smtp.Services;

public static class MarkReadCommand
{
    public static Command Create()
    {
        var idOption = new Option<string?>(
            "--id",
            "Email unique ID");

        var serverNameOption = new Option<string?>(
            "--servername",
            "Name of the configured server");

        var command = new Command("mark-read", "Mark an email as read")
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

            var success = emailService.MarkEmailAsReadAsync(id).GetAwaiter().GetResult();

            var result = new MarkReadResult
            {
                Success = success,
                Id = id
            };

            var json = JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);

            return success ? 0 : 2;
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
