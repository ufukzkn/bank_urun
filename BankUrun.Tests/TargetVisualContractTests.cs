namespace BankUrun.Tests;

public class TargetVisualContractTests
{
    [Fact]
    public void SelectedExport_IsOptInAndPostsContextKeys()
    {
        var index = ReadWebFile("Views", "Targets", "Index.cshtml");
        var rows = ReadWebFile("Views", "Targets", "_TargetRows.cshtml");
        var client = ReadWebFile("wwwroot", "js", "site.js");

        Assert.Contains("data-target-selection-toggle", index);
        Assert.Contains("data-target-selection-toolbar hidden", index);
        Assert.Contains("data-target-select-page", index);
        Assert.Contains("data-target-select-row", rows);
        Assert.Contains("selectedTargetContexts = new Set()", client);
        Assert.Contains("addValue(\"ContextKeys\", contextKey)", client);
        Assert.Contains("form.method = \"post\"", client);
        Assert.Contains("data-target-selection-limit", index);
        Assert.Contains("selectedTargetContexts.size >= targetSelectionLimit", client);
    }

    [Fact]
    public void TargetEditor_RendersAndUpdatesTheMonthlyAllocationPreview()
    {
        var editor = ReadWebFile("Views", "Targets", "_TargetEditor.cshtml");
        var client = ReadWebFile("wwwroot", "js", "site.js");
        var stylesheet = ReadWebFile("wwwroot", "css", "target-visuals.css");

        Assert.Contains("data-target-calculation-type", editor);
        Assert.Contains("data-target-allocation-preview", editor);
        Assert.Equal(1, Count(editor, "data-target-allocation-chart"));
        Assert.Contains("updateTargetAllocationPreview", client);
        Assert.Contains("allocateTargetBlock", client);
        Assert.Contains(".target-allocation-chart", stylesheet);
        Assert.Contains("prefers-reduced-motion", stylesheet);
    }

    [Fact]
    public void TargetEditor_ShowsItsFullContextAndPortfolioTargetComposition()
    {
        var index = ReadWebFile("Views", "Targets", "Index.cshtml");
        var editor = ReadWebFile("Views", "Targets", "_TargetEditor.cshtml");
        var viewModel = ReadWebFile("ViewModels", "TargetViewModel.cs");
        var service = ReadWebFile("Services", "TargetManagementService.cs");
        var stylesheet = ReadWebFile("wwwroot", "css", "target-visuals.css");

        Assert.Contains("class=\"target-context-path\"", editor);
        Assert.Contains("@Model.GroupLabel", editor);
        Assert.Contains("@Model.BranchLabel", editor);
        Assert.Contains("@Model.ProductGamutLabel", editor);
        Assert.Contains("Portföy hedef bileşimi", editor);
        Assert.Contains("_ProductFlowVisual", editor);
        Assert.Contains("performance-visuals.css", index);
        Assert.Contains("PortfolioTargetProducts", viewModel);
        Assert.Contains("PortfolioTargetProducts = eligiblePortfolioRows", service);
        Assert.Contains("eligiblePortfolioRows.All(item => item.HasCompleteTarget)", service);
        Assert.Contains("item.HasCompleteTarget ? item.PeriodTarget : null", service);
        Assert.Contains("Kısmi toplam", ReadWebFile(
            "Views", "Shared", "_ProductFlowVisual.cshtml"));
        Assert.Contains(".target-context-path", stylesheet);
    }

    [Fact]
    public void TargetWorkbook_IsFilterableAndFormatsTargetAmounts()
    {
        var service = ReadWebFile("Services", "TargetManagementService.cs");

        Assert.Contains(".SetAutoFilter()", service);
        Assert.Contains("sheet.Column(9).Style.NumberFormat.Format = \"#,##0.00\"", service);
        Assert.Contains("LoadWorkbookTargetsAsync", service);
        Assert.Contains("keys.Chunk(100)", service);
        Assert.Contains("\"target-context-rows\"", service);
        Assert.Contains("new TargetAggregateFact(", service);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string ReadWebFile(params string[] pathParts)
    {
        var directory = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([directory, "BankUrun.Web", .. pathParts]));
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
