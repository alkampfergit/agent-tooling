namespace Smtp.Services;

using Smtp.Models;

public interface IEmailService
{
    Task<List<EmailSummary>> GetUnreadEmailsAsync();
    Task<EmailDetails?> GetEmailDetailsAsync(string emailId);
    Task<bool> MarkEmailAsReadAsync(string emailId);
    Task<List<(string Id, bool Success)>> MarkEmailsAsReadAsync(IEnumerable<string> emailIds);
}
