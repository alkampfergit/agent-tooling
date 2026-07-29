using Search.Commands;

return await SearchCommand.Create().Parse(args).InvokeAsync();
