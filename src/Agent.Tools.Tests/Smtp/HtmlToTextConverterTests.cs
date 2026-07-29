namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Services;

public class HtmlToTextConverterTests
{
    [Test]
    public void ConvertHtmlToText_StripsTags()
    {
        var converter = new HtmlToTextConverter();
        var html = "<p>Hello <strong>World</strong></p>";
        var result = converter.ConvertHtmlToText(html);
        Assert.That(result, Does.Contain("Hello"));
        Assert.That(result, Does.Contain("World"));
        Assert.That(result, Does.Not.Contain("<"));
    }

    [Test]
    public void ConvertHtmlToText_RemovesScriptTags()
    {
        var converter = new HtmlToTextConverter();
        var html = "<p>Visible</p><script>alert('hidden')</script>";
        var result = converter.ConvertHtmlToText(html);
        Assert.That(result, Does.Contain("Visible"));
        Assert.That(result, Does.Not.Contain("hidden"));
    }

    [Test]
    public void ConvertHtmlToText_RemovesStyleTags()
    {
        var converter = new HtmlToTextConverter();
        var html = "<style>body { color: red; }</style><p>Text</p>";
        var result = converter.ConvertHtmlToText(html);
        Assert.That(result, Does.Contain("Text"));
        Assert.That(result, Does.Not.Contain("color"));
    }

    [Test]
    public void ConvertHtmlToText_CollapseWhitespace()
    {
        var converter = new HtmlToTextConverter();
        var html = "<p>Hello   \n\n   World</p>";
        var result = converter.ConvertHtmlToText(html);
        Assert.That(result, Is.EqualTo("Hello World"));
    }

    [Test]
    public void ConvertHtmlToText_EmptyString()
    {
        var converter = new HtmlToTextConverter();
        var result = converter.ConvertHtmlToText("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertHtmlToText_MalformedHtml_FallsBack()
    {
        var converter = new HtmlToTextConverter();
        var malformed = "<p>Unclosed paragraph<div>No closing";
        var result = converter.ConvertHtmlToText(malformed);
        Assert.That(result, Does.Contain("Unclosed"));
    }
}
