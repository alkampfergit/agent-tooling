namespace Smtp.Configuration;

public class ServerConfig
{
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required bool UseHttps { get; set; }
}
