using System.Xml.Linq;

namespace BankUrun.Tests;

public class FaviconContractTests
{
    [Fact]
    public void Layout_UsesTransparentSvgAndFallbackFaviconAssets()
    {
        var root = FindRepositoryRoot();
        var layout = File.ReadAllText(Path.Combine(
            root, "BankUrun.Web", "Views", "Shared", "_Layout.cshtml"));
        var webRoot = Path.Combine(root, "BankUrun.Web", "wwwroot");

        Assert.Contains("type=\"image/svg+xml\"", layout);
        Assert.Contains("href=\"~/favicon.svg\"", layout);
        Assert.Contains("href=\"~/favicon-32x32.png\"", layout);
        Assert.Contains("href=\"~/favicon.ico\"", layout);
        Assert.Contains("href=\"~/apple-touch-icon.png\"", layout);
        Assert.DoesNotContain("type=\"image/jpeg\"", layout);
        Assert.True(File.Exists(Path.Combine(webRoot, "favicon-32x32.png")));
        Assert.True(File.Exists(Path.Combine(webRoot, "apple-touch-icon.png")));
        Assert.True(File.Exists(Path.Combine(webRoot, "favicon.ico")));
    }

    [Fact]
    public void SvgFavicon_HasNoBackgroundShape()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root, "BankUrun.Web", "wwwroot", "favicon.svg"));
        var svg = Assert.IsType<XElement>(document.Root);

        Assert.Equal("-11 0 112 112", svg.Attribute("viewBox")?.Value);
        Assert.DoesNotContain(svg.Descendants(), element =>
            element.Name.LocalName is "rect" or "image");
        var path = Assert.Single(svg.Descendants(), element =>
            element.Name.LocalName == "path");
        Assert.Equal("#005696", path.Attribute("fill")?.Value);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "BankUrun.Web")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Proje kökü bulunamadı.");
    }
}
