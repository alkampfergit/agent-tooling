namespace Smtp.Services;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Smtp.Configuration;
using Smtp.Models;
using static MailKit.UniqueId;
using MailKit.Security;

public class ImapEmailService : IEmailService
{
    private readonly ServerConfig _server;
    private readonly Func<ServerConfig, IImapClient> _clientFactory;

    public ImapEmailService(ServerConfig server)
        : this(server, ImapClientFactory.CreateClient)
    {
    }

    internal ImapEmailService(ServerConfig server, Func<ServerConfig, IImapClient> clientFactory)
    {
        _server = server;
        _clientFactory = clientFactory;
    }

    public async Task<List<EmailSummary>> GetUnreadEmailsAsync()
    {
        using var client = _clientFactory(_server);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        var summaries = new List<EmailSummary>();
        var uids = await inbox.SearchAsync(SearchQuery.NotSeen);

        foreach (var uid in uids)
        {
            var message = await inbox.GetMessageAsync(uid);
            summaries.Add(new EmailSummary
            {
                Id = uid.ToString(),
                From = message.From.ToString(),
                Subject = message.Subject ?? "(no subject)",
                Date = message.Date.DateTime,
                Preview = ExtractPreview(message)
            });
        }

        return summaries;
    }

    public async Task<EmailDetails?> GetEmailDetailsAsync(string emailId)
    {
        if (!uint.TryParse(emailId, out var uidValue))
            throw new InvalidOperationException($"Invalid email ID: {emailId}");

        using var client = _clientFactory(_server);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        var uid = new UniqueId(uidValue);
        MimeMessage? message;
        try
        {
            message = await inbox.GetMessageAsync(uid);
        }
        catch (MessageNotFoundException)
        {
            return null;
        }

        if (message == null)
            return null;

        var body = ExtractBody(message);
        if (!string.IsNullOrEmpty(body) && IsHtmlContent(message))
        {
            body = HtmlToTextConverter.ConvertHtmlToText(body);
        }

        return new EmailDetails
        {
            Id = uid.ToString(),
            From = message.From.ToString(),
            To = ExtractAddresses(message.To),
            Cc = ExtractAddresses(message.Cc),
            Subject = message.Subject ?? "(no subject)",
            Date = message.Date.DateTime,
            Body = body ?? ""
        };
    }

    public async Task<bool> MarkEmailAsReadAsync(string emailId)
    {
        if (!uint.TryParse(emailId, out _))
            throw new InvalidOperationException($"Invalid email ID: {emailId}");

        var results = await MarkEmailsAsReadAsync(new[] { emailId });
        return results[0].Success;
    }

    public async Task<List<(string Id, bool Success)>> MarkEmailsAsReadAsync(IEnumerable<string> emailIds)
    {
        using var client = _clientFactory(_server);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        var results = new List<(string Id, bool Success)>();
        foreach (var emailId in emailIds)
        {
            results.Add((emailId, MarkSingleAsRead(inbox, emailId)));
        }

        return results;
    }

    private static bool MarkSingleAsRead(IMailFolder inbox, string emailId)
    {
        if (!uint.TryParse(emailId, out var uidValue))
            return false;

        var uid = new UniqueId(uidValue);
        MimeMessage? message;
        try
        {
            message = inbox.GetMessage(uid);
        }
        catch (MessageNotFoundException)
        {
            return false;
        }

        if (message == null)
            return false;

        inbox.SetFlags(uid, MessageFlags.Seen, true);
        return true;
    }

    private static string ExtractPreview(MimeMessage message)
    {
        var body = ExtractBody(message);
        if (string.IsNullOrWhiteSpace(body))
            return "";

        if (IsHtmlContent(message))
        {
            body = HtmlToTextConverter.ConvertHtmlToText(body);
        }

        return body.Length > 200 ? body.Substring(0, 200).Trim() + "..." : body.Trim();
    }

    private static string? ExtractBody(MimeMessage message)
    {
        if (message.HtmlBody != null)
            return message.HtmlBody;

        if (message.TextBody != null)
            return message.TextBody;

        return null;
    }

    private static bool IsHtmlContent(MimeMessage message)
    {
        return message.HtmlBody != null;
    }

    private static List<string> ExtractAddresses(InternetAddressList addresses)
    {
        return addresses
            .OfType<MailboxAddress>()
            .Select(a => a.Address)
            .ToList();
    }
}
