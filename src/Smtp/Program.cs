using System.CommandLine;
using Smtp.Commands;

RootCommand rootCommand = new("SMTP email CLI tool")
{
    SummaryCommand.Create(),
    GetDetailsCommand.Create(),
    MarkReadCommand.Create()
};

return rootCommand.Parse(args).Invoke();
