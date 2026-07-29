using System.CommandLine;
using Smtp.Commands;

RootCommand rootCommand = new("SMTP email CLI tool for reading and managing emails via IMAP protocol. Configuration via appsettings.json or agent-tooling.json")
{
    SummaryCommand.Create(),
    GetDetailsCommand.Create(),
    MarkReadCommand.Create()
};

return rootCommand.Parse(args).Invoke();
