namespace Smtp.Services;

using Smtp.Configuration;

public static class EmailServiceFactory
{
    public static IEmailService Create(ServerConfig server)
    {
        if (string.Equals(server.Type, "Imap", StringComparison.OrdinalIgnoreCase))
            return new ImapEmailService(server);

        if (string.Equals(server.Type, "Office365", StringComparison.OrdinalIgnoreCase))
            return new GraphEmailService(server);

        throw new InvalidOperationException(
            $"Unknown server type '{server.Type}' for server '{server.Name}'.");
    }
}
