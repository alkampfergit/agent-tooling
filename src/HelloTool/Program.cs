Console.WriteLine("Hello from Agent Tools!");
Console.WriteLine($"Tool: HelloTool");

if (args.Length > 0)
{
    Console.WriteLine($"Arguments: {string.Join(", ", args)}");
}
