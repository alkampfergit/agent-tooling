namespace Smtp.Configuration;

public class ServerConfig
{
    public required string Name { get; set; }
    public string Type { get; set; } = "Imap";
    public bool Default { get; set; } = false;
    public string? Address { get; set; }
    public int? Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? UseHttps { get; set; }
    public string? ClientId { get; set; }
    public string TenantId { get; set; } = "common";

    public void Validate()
    {
        if (string.Equals(Type, "Imap", StringComparison.OrdinalIgnoreCase))
        {
            ValidateImap();
        }
        else if (string.Equals(Type, "Office365", StringComparison.OrdinalIgnoreCase))
        {
            ValidateOffice365();
        }
        else
        {
            throw new InvalidOperationException(
                $"Server '{Name}' is not configured correctly: unknown Type '{Type}'. Expected 'Imap' or 'Office365'.");
        }
    }

    private void ValidateImap()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Address)) missing.Add(nameof(Address));
        if (Port == null) missing.Add(nameof(Port));
        if (string.IsNullOrWhiteSpace(Username)) missing.Add(nameof(Username));
        if (string.IsNullOrWhiteSpace(Password)) missing.Add(nameof(Password));
        if (UseHttps == null) missing.Add(nameof(UseHttps));

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Server '{Name}' (Imap) is not fully configured: missing {string.Join(", ", missing)}.");
    }

    private void ValidateOffice365()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Username)) missing.Add(nameof(Username));
        if (string.IsNullOrWhiteSpace(ClientId)) missing.Add(nameof(ClientId));

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Server '{Name}' (Office365) is not fully configured: missing {string.Join(", ", missing)}.");

        var notAllowed = new List<string>();
        if (!string.IsNullOrWhiteSpace(Address)) notAllowed.Add(nameof(Address));
        if (Port != null) notAllowed.Add(nameof(Port));
        if (!string.IsNullOrWhiteSpace(Password)) notAllowed.Add(nameof(Password));
        if (UseHttps != null) notAllowed.Add(nameof(UseHttps));

        if (notAllowed.Count > 0)
            throw new InvalidOperationException(
                $"Server '{Name}' (Office365) is not fully configured: {string.Join(", ", notAllowed)} must not be set for Office365 servers.");
    }
}
