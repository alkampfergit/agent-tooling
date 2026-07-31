namespace Smtp.Services;

using HtmlAgilityPack;
using System.Text.RegularExpressions;

public class HtmlToTextConverter
{
    public string ConvertHtmlToText(string html)
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
            text = text.Replace("​", "");
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
        catch
        {
            return Regex.Replace(html, "<[^>]+>", "").Trim();
        }
    }
}
