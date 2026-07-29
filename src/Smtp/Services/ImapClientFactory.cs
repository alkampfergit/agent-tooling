namespace Smtp.Services;

using MailKit.Net.Imap;
using Smtp.Configuration;

public class ImapClientFactory
{
    public static IImapClient CreateClient(ServerConfig server)
    {
        var client = new ImapClient();
        try
        {
            client.Connect(server.Address, server.Port, server.UseHttps);
            client.Authenticate(server.Username, server.Password);
            return client;
        }
        catch (Exception ex)
        {
            client.Dispose();
            throw new InvalidOperationException(
                $"Failed to connect to {server.Address}:{server.Port} " +
                $"as {server.Username}: {ex.Message}", ex);
        }
    }
}
