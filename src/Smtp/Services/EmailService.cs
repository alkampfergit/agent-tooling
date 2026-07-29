namespace Smtp.Services;

using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Smtp.Configuration;
using Smtp.Models;
using static MailKit.UniqueId;
using MailKit.Security;

public class EmailService
{
    private readonly ServerConfig _server;
    private readonly HtmlToTextConverter _htmlConverter;

    public EmailService(ServerConfig server, HtmlToTextConverter htmlConverter)
    {
        _server = server;
        _htmlConverter = htmlConverter;
    }

    public async Task<List<EmailSummary>> GetUnreadEmailsAsync()
    {
        using var client = ImapClientFactory.CreateClient(_server);
        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadOnly);

        var summaries = new List<EmailSummary>();
        var uids = inbox.Search(SearchQuery.NotSeen);

        foreach (var uid in uids)
        {
            var message = inbox.GetMessage(uid);
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

        using var client = ImapClientFactory.CreateClient(_server);
        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadOnly);

        var uid = new UniqueId(uidValue);
        MimeMessage? message;
        try
        {
            message = inbox.GetMessage(uid);
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
            body = _htmlConverter.ConvertHtmlToText(body);
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
        if (!uint.TryParse(emailId, out var uidValue))
            throw new InvalidOperationException($"Invalid email ID: {emailId}");

        using var client = ImapClientFactory.CreateClient(_server);
        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadWrite);

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

    private string ExtractPreview(MimeMessage message)
    {
        var body = ExtractBody(message);
        if (string.IsNullOrWhiteSpace(body))
            return "";

        if (IsHtmlContent(message))
        {
            body = _htmlConverter.ConvertHtmlToText(body);
        }

        return body.Length > 200 ? body.Substring(0, 200).Trim() + "..." : body.Trim();
    }

    private string? ExtractBody(MimeMessage message)
    {
        if (message.HtmlBody != null)
            return message.HtmlBody;

        if (message.TextBody != null)
            return message.TextBody;

        return null;
    }

    private bool IsHtmlContent(MimeMessage message)
    {
        return message.HtmlBody != null;
    }

    private List<string> ExtractAddresses(InternetAddressList addresses)
    {
        return addresses
            .OfType<MailboxAddress>()
            .Select(a => a.Address)
            .ToList();
    }
}
