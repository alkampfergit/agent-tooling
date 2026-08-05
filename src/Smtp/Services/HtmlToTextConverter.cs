namespace Smtp.Services;

using HtmlAgilityPack;
using System.Text.RegularExpressions;

public static partial class HtmlToTextConverter
{
    public static string ConvertHtmlToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scriptAndStyleNodes = doc.DocumentNode.SelectNodes("//script | //style");
            if (scriptAndStyleNodes != null)
            {
                foreach (var node in scriptAndStyleNodes)
                {
                    node.Remove();
                }
            }

            var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
            text = text.Replace("\u200B", "");
            text = WhitespaceRegex().Replace(text, " ");
            return text.Trim();
        }
        catch
        {
            return HtmlTagRegex().Replace(html, "").Trim();
        }
    }

    [GeneratedRegex(@"\s+", RegexOptions.None, 1000)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.None, 1000)]
    private static partial Regex HtmlTagRegex();
}
