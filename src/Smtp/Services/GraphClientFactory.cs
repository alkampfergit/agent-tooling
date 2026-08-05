namespace Smtp.Services;

using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Smtp.Configuration;

public static class GraphClientFactory
{
    private static readonly string[] Scopes = { "https://graph.microsoft.com/Mail.ReadWrite" };
    private const string CacheName = "AgentTooling.Smtp.Office365";

    public static GraphServiceClient CreateClient(ServerConfig server)
    {
        var credential = CreateCredential(server);
        return new GraphServiceClient(credential, Scopes);
    }

    private static DeviceCodeCredential CreateCredential(ServerConfig server)
    {
        var authRecordPath = GetAuthRecordPath(server);

        var options = new DeviceCodeCredentialOptions
        {
            ClientId = server.ClientId!,
            TenantId = server.TenantId,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = CacheName
            },
            DeviceCodeCallback = (info, _) =>
            {
                Console.Error.WriteLine(info.Message);
                return Task.CompletedTask;
            }
        };

        if (File.Exists(authRecordPath))
        {
            using var readStream = File.OpenRead(authRecordPath);
            options.AuthenticationRecord = AuthenticationRecord.DeserializeAsync(readStream).GetAwaiter().GetResult();
            return new DeviceCodeCredential(options);
        }

        var credential = new DeviceCodeCredential(options);
        var authRecord = credential.AuthenticateAsync(new TokenRequestContext(Scopes)).GetAwaiter().GetResult();

        var directory = Path.GetDirectoryName(authRecordPath)!;
        Directory.CreateDirectory(directory);
        using (var writeStream = File.Create(authRecordPath))
        {
            authRecord.SerializeAsync(writeStream).GetAwaiter().GetResult();
        }

        return credential;
    }

    private static string GetAuthRecordPath(ServerConfig server)
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "AgentTooling", "Smtp", $"{server.Name}.authrecord.json");
    }
}
