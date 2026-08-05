namespace Smtp.Services;

using Smtp.Configuration;

public static class EmailServiceFactory
{
    public static IEmailService Create(ServerConfig server, HtmlToTextConverter htmlConverter)
    {
        if (string.Equals(server.Type, "Imap", StringComparison.OrdinalIgnoreCase))
            return new ImapEmailService(server, htmlConverter);

        if (string.Equals(server.Type, "Office365", StringComparison.OrdinalIgnoreCase))
            return new GraphEmailService(server, htmlConverter);

        throw new InvalidOperationException(
            $"Unknown server type '{server.Type}' for server '{server.Name}'.");
    }
}
