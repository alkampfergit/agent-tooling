namespace Smtp.Services;

using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Smtp.Configuration;
using EmailDetails = Smtp.Models.EmailDetails;
using EmailSummary = Smtp.Models.EmailSummary;

public class GraphEmailService : IEmailService
{
    private static readonly string[] SummarySelect = { "id", "from", "subject", "receivedDateTime", "bodyPreview" };
    private static readonly string[] SummaryOrderby = { "receivedDateTime desc" };
    private static readonly string[] DetailsSelect = { "id", "from", "toRecipients", "ccRecipients", "subject", "receivedDateTime", "body" };

    private readonly ServerConfig _server;
    private readonly HtmlToTextConverter _htmlConverter;
    private readonly Func<ServerConfig, GraphServiceClient> _clientFactory;

    public GraphEmailService(ServerConfig server, HtmlToTextConverter htmlConverter)
        : this(server, htmlConverter, GraphClientFactory.CreateClient)
    {
    }

    internal GraphEmailService(ServerConfig server, HtmlToTextConverter htmlConverter, Func<ServerConfig, GraphServiceClient> clientFactory)
    {
        _server = server;
        _htmlConverter = htmlConverter;
        _clientFactory = clientFactory;
    }

    public Task<List<EmailSummary>> GetUnreadEmailsAsync()
    {
        return ExecuteAsync(async () =>
        {
            var client = _clientFactory(_server);
            var messages = await client.Me.MailFolders["inbox"].Messages.GetAsync(cfg =>
            {
                cfg.QueryParameters.Filter = "isRead eq false";
                cfg.QueryParameters.Select = SummarySelect;
                cfg.QueryParameters.Orderby = SummaryOrderby;
            });

            var summaries = new List<EmailSummary>();
            if (messages?.Value == null)
                return summaries;

            foreach (var message in messages.Value)
            {
                var preview = message.BodyPreview ?? "";
                summaries.Add(new EmailSummary
                {
                    Id = message.Id ?? "",
                    From = ExtractAddress(message.From),
                    Subject = message.Subject ?? "(no subject)",
                    Date = message.ReceivedDateTime?.UtcDateTime ?? default,
                    Preview = preview.Length > 200 ? preview.Substring(0, 200).Trim() + "..." : preview.Trim()
                });
            }

            return summaries;
        });
    }

    public Task<EmailDetails?> GetEmailDetailsAsync(string emailId)
    {
        return ExecuteAsync(async () =>
        {
            var client = _clientFactory(_server);
            Message? message;
            try
            {
                message = await client.Me.Messages[emailId].GetAsync(cfg =>
                {
                    cfg.QueryParameters.Select = DetailsSelect;
                });
            }
            catch (ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                return null;
            }

            if (message == null)
                return null;

            var body = message.Body?.Content ?? "";
            if (message.Body?.ContentType == BodyType.Html)
            {
                body = _htmlConverter.ConvertHtmlToText(body);
            }

            return new EmailDetails
            {
                Id = message.Id ?? emailId,
                From = ExtractAddress(message.From),
                To = ExtractAddresses(message.ToRecipients),
                Cc = ExtractAddresses(message.CcRecipients),
                Subject = message.Subject ?? "(no subject)",
                Date = message.ReceivedDateTime?.UtcDateTime ?? default,
                Body = body
            };
        });
    }

    public async Task<bool> MarkEmailAsReadAsync(string emailId)
    {
        var results = await MarkEmailsAsReadAsync(new[] { emailId });
        return results[0].Success;
    }

    public Task<List<(string Id, bool Success)>> MarkEmailsAsReadAsync(IEnumerable<string> emailIds)
    {
        return ExecuteAsync(async () =>
        {
            var client = _clientFactory(_server);
            var results = new List<(string Id, bool Success)>();

            foreach (var emailId in emailIds)
            {
                results.Add((emailId, await MarkSingleAsReadAsync(client, emailId)));
            }

            return results;
        });
    }

    private static async Task<bool> MarkSingleAsReadAsync(GraphServiceClient client, string emailId)
    {
        try
        {
            await client.Me.Messages[emailId].PatchAsync(new Message { IsRead = true });
            return true;
        }
        catch (ODataError)
        {
            return false;
        }
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (CredentialUnavailableException ex)
        {
            throw new InvalidOperationException(
                $"Cannot persist the Office 365 sign-in token securely for server '{_server.Name}'. " +
                "On Linux this requires a Secret Service provider (gnome-keyring or kwallet) to be installed and running. " +
                $"Details: {ex.Message}", ex);
        }
    }

    private static string ExtractAddress(Recipient? recipient) => recipient?.EmailAddress?.Address ?? "";

    private static List<string> ExtractAddresses(List<Recipient>? recipients)
    {
        if (recipients == null)
            return new();

        return recipients
            .Select(r => r.EmailAddress?.Address)
            .Where(a => !string.IsNullOrEmpty(a))
            .Select(a => a!)
            .ToList();
    }
}
