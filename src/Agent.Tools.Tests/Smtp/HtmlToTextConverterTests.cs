namespace Agent.Tools.Tests.Smtp;

using global::Smtp.Services;

[TestFixture]
[Category("smtp")]
[Category("Unit")]
public class HtmlToTextConverterTests
{
    [Test]
    public void ConvertHtmlToText_StripsTags()
    {
        var html = "<p>Hello <strong>World</strong></p>";
        var result = HtmlToTextConverter.ConvertHtmlToText(html);
        Assert.That(result, Does.Contain("Hello"));
        Assert.That(result, Does.Contain("World"));
        Assert.That(result, Does.Not.Contain("<"));
    }

    [Test]
    public void ConvertHtmlToText_RemovesScriptTags()
    {
        var html = "<p>Visible</p><script>alert('hidden')</script>";
        var result = HtmlToTextConverter.ConvertHtmlToText(html);
        Assert.That(result, Does.Contain("Visible"));
        Assert.That(result, Does.Not.Contain("hidden"));
    }

    [Test]
    public void ConvertHtmlToText_RemovesStyleTags()
    {
        var html = "<style>body { color: red; }</style><p>Text</p>";
        var result = HtmlToTextConverter.ConvertHtmlToText(html);
        Assert.That(result, Does.Contain("Text"));
        Assert.That(result, Does.Not.Contain("color"));
    }

    [Test]
    public void ConvertHtmlToText_CollapseWhitespace()
    {
        var html = "<p>Hello   \n\n   World</p>";
        var result = HtmlToTextConverter.ConvertHtmlToText(html);
        Assert.That(result, Is.EqualTo("Hello World"));
    }

    [Test]
    public void ConvertHtmlToText_EmptyString()
    {
        var result = HtmlToTextConverter.ConvertHtmlToText("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ConvertHtmlToText_MalformedHtml_FallsBack()
    {
        var malformed = "<p>Unclosed paragraph<div>No closing";
        var result = HtmlToTextConverter.ConvertHtmlToText(malformed);
        Assert.That(result, Does.Contain("Unclosed"));
    }

    [Test]
    public void ConvertHtmlToText_DecodesHtmlEntities()
    {
        var html = "<p>MVP&nbsp;PGI&nbsp;Events &amp; More</p>";
        var result = HtmlToTextConverter.ConvertHtmlToText(html);
        Assert.That(result, Does.Not.Contain("&nbsp;"));
        Assert.That(result, Does.Not.Contain("&amp;"));
        Assert.That(result, Does.Contain("MVP PGI Events & More"));
    }

    [Test]
    public void ConvertHtmlToText_RemovesZeroWidthSpaces()
    {
        var html = "<p>​Redmond</p>";
        var result = HtmlToTextConverter.ConvertHtmlToText(html);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Does.Not.Contain('​'));
            Assert.That(result, Is.EqualTo("Redmond"));
        }
    }
}
