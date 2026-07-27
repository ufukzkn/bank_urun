namespace BankUrun.Tests;

public class ManagementVisualContractTests
{
    [Fact]
    public void ConfirmationDialog_RendersRelationshipImpactMap()
    {
        var layout = ReadWebFile("Views", "Shared", "_Layout.cshtml");
        var client = ReadWebFile("wwwroot", "js", "site.js");
        var stylesheet = ReadWebFile("wwwroot", "css", "management-visuals.css");

        Assert.Contains("~/css/management-visuals.css", layout);
        Assert.Contains("impactCountTone", client);
        Assert.Contains("impact-relationship-map", client);
        Assert.Contains("escapeHtml(result?.subject", client);
        Assert.Contains(".impact-map-outcomes", stylesheet);
        Assert.Contains(".impact-map-node.is-preserved", stylesheet);
        Assert.Contains(".impact-map-node.is-removed", stylesheet);
    }

    [Fact]
    public void ImpactMap_IsResponsiveAndHonorsReducedMotion()
    {
        var stylesheet = ReadWebFile("wwwroot", "css", "management-visuals.css");

        Assert.Contains("@media (max-width: 720px)", stylesheet);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", stylesheet);
    }

    private static string ReadWebFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var webProject = Path.Combine(directory.FullName, "BankUrun.Web");
            if (Directory.Exists(webProject))
            {
                return File.ReadAllText(Path.Combine([webProject, .. pathParts]));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("BankUrun.Web proje klasörü bulunamadı.");
    }
}
