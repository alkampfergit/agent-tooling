namespace Smtp.Commands;

using System.CommandLine;
using System.Text.Json;
using Smtp.Configuration;
using Smtp.Models;
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
        return ExecuteCore(id, emailId =>
        {
            var configProvider = new ConfigurationProvider();
            var server = configProvider.GetServer(serverName);

            var emailService = EmailServiceFactory.Create(server);

            return emailService.GetEmailDetailsAsync(emailId);
        });
    }

    internal static int ExecuteCore(string? id, Func<string, Task<EmailDetails?>> getDetails)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.Error.WriteLine("--id is required");
                return 2;
            }

            var details = getDetails(id).GetAwaiter().GetResult();
            if (details == null)
            {
                Console.Error.WriteLine($"Email ID {id} not found");
                return 2;
            }

            // Compact JSON for single objects (no indentation for token efficiency)
            var json = JsonSerializer.Serialize(details);
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
            Console.Error.WriteLine($"Error: {ExceptionFormatting.Chain(ex)}");
            return 2;
        }
    }
}
