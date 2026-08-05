namespace Smtp.Services;

public static class ExceptionFormatting
{
    public static string Chain(Exception ex)
    {
        var messages = new List<string>();
        var current = ex;
        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }
        return string.Join(" ---> ", messages);
    }
}
