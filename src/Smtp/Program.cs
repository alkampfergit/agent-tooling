using System.CommandLine;
using Smtp.Commands;

RootCommand rootCommand = new("Email CLI tool for reading and managing emails via IMAP or Office 365 (Microsoft Graph). Configuration via appsettings.json or agent-tooling.json")
{
    SummaryCommand.Create(),
    GetDetailsCommand.Create(),
    MarkReadCommand.Create()
};

return await rootCommand.Parse(args).InvokeAsync();
