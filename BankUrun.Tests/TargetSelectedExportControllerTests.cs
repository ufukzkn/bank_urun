using BankUrun.Web.Controllers;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Tests;

public class TargetSelectedExportControllerTests
{
    [Fact]
    public async Task ExportSelected_ExportsOnlyDistinctPostedContextKeys()
    {
        var service = new RecordingTargetManagementService();
        var controller = CreateController(service);
        var input = new TargetSelectedExportInput
        {
            EntryMode = TargetEntryMode.ThreeMonth,
            ContextKeys = ["12:34", "18:52", "12:34"]
        };

        var result = await controller.ExportSelected(input);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal([1, 2, 3], file.FileContents);
        Assert.Equal("selected.xlsx", file.FileDownloadName);
        Assert.Equal(TargetEntryMode.ThreeMonth, service.SelectedEntryMode);
        Assert.Equal(
            [
                new TargetContextKey(12, 34),
                new TargetContextKey(18, 52)
            ],
            service.SelectedContextKeys!.OrderBy(item => item.PortfolioId));
    }

    [Fact]
    public async Task ExportSelected_RejectsEmptySelectionWithoutCallingService()
    {
        var service = new RecordingTargetManagementService();
        var controller = CreateController(service);

        var result = await controller.ExportSelected(
            new TargetSelectedExportInput());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("en az bir", Assert.IsType<string>(badRequest.Value));
        Assert.Null(service.SelectedContextKeys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12:")]
    [InlineData("12:34:56")]
    [InlineData("abc:34")]
    [InlineData("0:34")]
    [InlineData("12:-1")]
    public async Task ExportSelected_RejectsMalformedContextKeys(string contextKey)
    {
        var service = new RecordingTargetManagementService();
        var controller = CreateController(service);

        var result = await controller.ExportSelected(
            new TargetSelectedExportInput { ContextKeys = [contextKey] });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.SelectedContextKeys);
    }

    [Fact]
    public async Task ExportSelected_RejectsSelectionAboveTheBound()
    {
        var service = new RecordingTargetManagementService();
        var controller = CreateController(service);
        var input = new TargetSelectedExportInput
        {
            ContextKeys = Enumerable.Range(
                    1,
                    TargetSelectedExportInput.MaximumContextCount + 1)
                .Select(index => $"{index}:{index}")
                .ToList()
        };

        var result = await controller.ExportSelected(input);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(
            TargetSelectedExportInput.MaximumContextCount.ToString(),
            Assert.IsType<string>(badRequest.Value));
        Assert.Null(service.SelectedContextKeys);
    }

    [Fact]
    public async Task ExportSelected_RejectsAContextThatServiceNoLongerFinds()
    {
        var service = new RecordingTargetManagementService
        {
            SelectedExportError = new InvalidOperationException(
                "Seçilen hedeflerden biri artık aktif veya geçerli değil.")
        };
        var controller = CreateController(service);

        var result = await controller.ExportSelected(
            new TargetSelectedExportInput { ContextKeys = ["12:34"] });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(
            "Seçilen hedeflerden biri artık aktif veya geçerli değil.",
            badRequest.Value);
    }

    [Fact]
    public void ExportSelected_IsPostOnlyAndAntiforgeryProtected()
    {
        var method = typeof(TargetsController).GetMethod(
            nameof(TargetsController.ExportSelected));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(
            typeof(HttpPostAttribute), inherit: true).SingleOrDefault());
        Assert.NotNull(method.GetCustomAttributes(
            typeof(ValidateAntiForgeryTokenAttribute), inherit: true)
            .SingleOrDefault());
    }

    private static TargetsController CreateController(
        RecordingTargetManagementService service)
    {
        return new TargetsController(service, new RecordingCacheInvalidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private sealed class RecordingCacheInvalidator : IPerformanceCacheInvalidator
    {
        public long Version => 0;
        public void Invalidate()
        {
        }
    }

    private sealed class RecordingTargetManagementService
        : ITargetManagementService
    {
        public IReadOnlyCollection<TargetContextKey>? SelectedContextKeys { get; private set; }
        public TargetEntryMode? SelectedEntryMode { get; private set; }
        public InvalidOperationException? SelectedExportError { get; init; }

        public Task<TargetWorkbookResult> ExportSelectedAsync(
            IReadOnlyCollection<TargetContextKey> contextKeys,
            TargetEntryMode entryMode,
            CancellationToken cancellationToken = default)
        {
            SelectedContextKeys = contextKeys;
            SelectedEntryMode = entryMode;
            if (SelectedExportError is not null)
            {
                throw SelectedExportError;
            }

            return Task.FromResult(
                new TargetWorkbookResult([1, 2, 3], "selected.xlsx"));
        }

        public Task<TargetIndexViewModel> GetIndexAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetPageViewModel> GetPageAsync(
            TargetQuery query,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetEditorViewModel> GetEditorAsync(
            int parameterId,
            int portfolioId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateTargetsAsync(
            TargetPeriodInput input,
            string actor,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetWorkbookResult> ExportAsync(
            TargetQuery query,
            TargetEntryMode entryMode,
            bool templateOnly,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TargetImportPreviewViewModel> PreviewImportAsync(
            Stream stream,
            string fileName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ConfirmImportAsync(
            string token,
            string actor,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
