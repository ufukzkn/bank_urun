using System.Globalization;
using System.Text;
using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class TargetManagementService(
    AppDbContext db,
    TimeProvider timeProvider,
    TargetImportPreviewStore importStore) : ITargetManagementService
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CompareInfo TurkishCompare = TurkishCulture.CompareInfo;
    private static readonly int[] AllowedPageSizes = [5, 10, 25, 50];
    private static readonly string[] TurkishMonthNames =
    [
        "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    public async Task<TargetIndexViewModel> GetIndexAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions.AsNoTracking()
            .OrderBy(item => item.GroupNo)
            .Select(item => new TargetGroupOptionViewModel
            {
                Id = item.Id,
                GroupNo = item.GroupNo,
                Name = item.Name
            }).ToListAsync(cancellationToken);
        var gamuts = await db.ProductGamuts.AsNoTracking()
            .Include(item => item.Group)
            .OrderBy(item => item.Group.GroupNo).ThenBy(item => item.Code)
            .Select(item => new TargetGamutOptionViewModel
            {
                Id = item.Id,
                GroupId = item.GroupId,
                GroupNo = item.Group.GroupNo,
                Code = item.Code,
                Name = item.Name
            }).ToListAsync(cancellationToken);
        var portfolios = await db.Portfolios.AsNoTracking()
            .Include(item => item.Branch)
            .Where(item => item.IsActive)
            .OrderBy(item => item.Branch.BranchCode).ThenBy(item => item.Code)
            .Select(item => new TargetPortfolioOptionViewModel
            {
                Id = item.Id,
                GroupId = item.GroupId,
                BranchId = item.BranchId,
                ProductGamutId = item.ProductGamutId,
                BranchCode = item.Branch.BranchCode,
                Code = item.Code,
                Name = item.Name
            }).ToListAsync(cancellationToken);
        var products = await db.ProductDefinitions.AsNoTracking()
            .Where(item => item.Type == ProductType.Main && item.IsActive)
            .OrderBy(item => item.Code)
            .Select(item => new TargetProductOptionViewModel
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name
            }).ToListAsync(cancellationToken);
        var years = await db.MainProductInstances.AsNoTracking()
            .Select(item => item.Year)
            .Distinct()
            .OrderByDescending(item => item)
            .ToListAsync(cancellationToken);

        return new TargetIndexViewModel
        {
            Groups = groups,
            ProductGamuts = gamuts,
            Portfolios = portfolios,
            Products = products,
            Years = years,
            Page = await GetPageAsync(new TargetQuery(), cancellationToken)
        };
    }

    public async Task<TargetPageViewModel> GetPageAsync(
        TargetQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildRowsAsync(query, cancellationToken);
        var pageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(rows.Count / (decimal)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);
        return new TargetPageViewModel
        {
            Rows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = rows.Count,
            TotalPages = totalPages
        };
    }

    public async Task<TargetEditorViewModel> GetEditorAsync(
        int parameterId,
        int portfolioId,
        CancellationToken cancellationToken = default)
    {
        var context = await ValidateContextAsync(parameterId, portfolioId, cancellationToken);
        var stored = await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
            .Where(item => item.PortfolioId == portfolioId
                && item.MainProductParameterId == parameterId)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        var termMonths = TargetPeriodValueConverter.GetTermMonths(
            context.Parameter.MainProductInstance.Term);
        var values = termMonths
            .Select(month => stored.GetValueOrDefault(month)?.TargetValue ?? 0)
            .ToList();

        var eligiblePortfolioRows = await BuildRowsAsync(
            new TargetQuery
            {
                PortfolioId = portfolioId,
                Year = context.Parameter.MainProductInstance.Year,
                Term = context.Parameter.MainProductInstance.Term,
                PageSize = 50
            },
            cancellationToken);
        var portfolioPeriodTarget = eligiblePortfolioRows.Sum(item => item.PeriodTarget);

        return new TargetEditorViewModel
        {
            ParameterId = parameterId,
            PortfolioId = portfolioId,
            Year = context.Parameter.MainProductInstance.Year,
            Term = context.Parameter.MainProductInstance.Term,
            CalculationType = context.Parameter.CalculationType,
            PortfolioLabel = $"{context.Portfolio.Code} - {context.Portfolio.Name}",
            MainProductLabel =
                $"{context.Parameter.MainProductInstance.MainProduct.Code} - {context.Parameter.MainProductInstance.MainProduct.Name}",
            SixMonthTarget = TargetPeriodValueConverter.Aggregate(
                values, context.Parameter.CalculationType),
            FirstThreeMonthTarget = TargetPeriodValueConverter.Aggregate(
                values.Take(3), context.Parameter.CalculationType),
            SecondThreeMonthTarget = TargetPeriodValueConverter.Aggregate(
                values.Skip(3), context.Parameter.CalculationType),
            PortfolioPeriodTarget = Round(portfolioPeriodTarget),
            Months = termMonths.Select(month => new TargetMonthViewModel
            {
                Month = month,
                MonthName = TurkishMonthNames[month],
                TargetValue = stored.GetValueOrDefault(month)?.TargetValue ?? 0,
                HasStoredTarget = stored.ContainsKey(month)
            }).ToList()
        };
    }

    public async Task UpdateTargetsAsync(
        TargetPeriodInput input,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var context = await ValidateContextAsync(
            input.ParameterId, input.PortfolioId, cancellationToken);
        var values = TargetPeriodValueConverter.Expand(
            context.Parameter.MainProductInstance.Term,
            context.Parameter.CalculationType,
            input.EntryMode,
            input.SixMonthTarget,
            input.FirstThreeMonthTarget,
            input.SecondThreeMonthTarget,
            input.Months.Select(item => new MonthlyTargetValue(
                item.Month, item.TargetValue)).ToList());

        await UpsertMonthlyValuesAsync(
            context,
            values,
            actor,
            $"UpdatePortfolioMainProductTargets:{input.EntryMode}",
            cancellationToken);
    }

    public async Task<TargetWorkbookResult> ExportAsync(
        TargetQuery query,
        TargetEntryMode entryMode,
        bool templateOnly,
        CancellationToken cancellationToken = default)
    {
        var rows = templateOnly
            ? []
            : await BuildRowsAsync(query, cancellationToken);
        var keys = rows.Select(row => (row.PortfolioId, row.ParameterId)).ToHashSet();
        var targets = keys.Count == 0
            ? []
            : await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
                .Where(item => rows.Select(row => row.PortfolioId).Contains(item.PortfolioId)
                    && rows.Select(row => row.ParameterId).Contains(item.MainProductParameterId))
                .ToListAsync(cancellationToken);
        var targetLookup = targets
            .Where(item => keys.Contains((item.PortfolioId, item.MainProductParameterId)))
            .GroupBy(item => (item.PortfolioId, item.MainProductParameterId))
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.Month, item => item.TargetValue));

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Hedefler");
        var headers = new[]
        {
            "Yıl", "Dönem", "Grup Kodu", "Şube Kodu", "Portföy Kodu",
            "Ana Ürün Kodu", "Giriş Tipi", "Aralık", "Hedef Tutarı"
        };
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        var excelRow = 2;
        foreach (var row in rows)
        {
            targetLookup.TryGetValue((row.PortfolioId, row.ParameterId), out var monthMap);
            monthMap ??= [];
            foreach (var value in BuildExportValues(row, entryMode, monthMap))
            {
                sheet.Cell(excelRow, 1).Value = row.Year;
                sheet.Cell(excelRow, 2).Value = row.Term;
                sheet.Cell(excelRow, 3).Value = row.GroupNo;
                sheet.Cell(excelRow, 4).Value = row.BranchCode;
                sheet.Cell(excelRow, 5).Value = row.PortfolioCode;
                sheet.Cell(excelRow, 6).Value = row.MainProductCode;
                sheet.Cell(excelRow, 7).Value = EntryModeLabel(entryMode);
                sheet.Cell(excelRow, 8).Value = value.Range;
                if (value.HasValue)
                {
                    sheet.Cell(excelRow, 9).Value = value.Value;
                }
                excelRow++;
            }
        }

        var explanation = workbook.Worksheets.Add("Açıklamalar");
        explanation.Cell("A1").Value = "Giriş Tipi";
        explanation.Cell("B1").Value = "Geçerli Aralık değerleri";
        explanation.Cell("A2").Value = "6 Aylık";
        explanation.Cell("B2").Value = "Dönem";
        explanation.Cell("A3").Value = "3 Aylık";
        explanation.Cell("B3").Value = "İlk 3 Ay, İkinci 3 Ay";
        explanation.Cell("A4").Value = "Aylık";
        explanation.Cell("B4").Value = "Ocak-Aralık; seçili dönemin altı ayı eksiksiz olmalıdır.";
        explanation.Cell("A6").Value =
            "Aynı ürün farklı grup veya portföylerde ayrı hedef bağlamıdır.";
        explanation.Cell("A7").Value =
            "Kümülatif 6/3 aylık tutarlar aylara bölünür; ortalamalı tutarlar aylara aynen uygulanır.";

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCECF5");
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        explanation.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var suffix = templateOnly ? "sablon" : DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        return new TargetWorkbookResult(
            stream.ToArray(),
            $"hedef-girisi-{suffix}.xlsx");
    }

    public async Task<TargetImportPreviewViewModel> PreviewImportAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        List<RawImportRow> rawRows;
        try
        {
            rawRows = ReadWorkbook(stream, errors);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new TargetImportPreviewViewModel
            {
                FileName = fileName,
                Errors = [$"Excel dosyası okunamadı: {ex.Message}"]
            };
        }

        if (rawRows.Count > 10_000)
        {
            errors.Add("Bir dosyada en fazla 10.000 hedef satırı bulunabilir.");
        }

        var commands = errors.Count == 0
            ? await ResolveImportCommandsAsync(rawRows, errors, cancellationToken)
            : [];
        var previewRows = commands.Take(20).Select(command =>
        {
            var calculationType = command.CalculationType;
            return new TargetImportPreviewRowViewModel
            {
                Context = command.Context,
                EntryMode = command.EntryMode,
                PeriodTarget = TargetPeriodValueConverter.Aggregate(
                    command.Months.Select(item => item.TargetValue),
                    calculationType)
            };
        }).ToList();
        var storedCommands = commands.Select(command => new TargetImportCommand(
            command.ParameterId,
            command.PortfolioId,
            command.Months,
            command.Context,
            command.EntryMode)).ToList();
        var token = errors.Count == 0 && storedCommands.Count > 0
            ? importStore.Put(storedCommands)
            : string.Empty;

        return new TargetImportPreviewViewModel
        {
            Token = token,
            FileName = fileName,
            SourceRowCount = rawRows.Count,
            TargetContextCount = commands.Count,
            MonthlyValueCount = commands.Sum(command => command.Months.Count),
            Errors = errors,
            Rows = previewRows
        };
    }

    public async Task ConfirmImportAsync(
        string token,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var commands = importStore.Take(token);
        if (commands.Count == 0)
        {
            throw new InvalidOperationException("İçe aktarılacak hedef bulunamadı.");
        }

        await ValidateStoredCommandsAsync(commands, cancellationToken);
        var portfolioIds = commands.Select(item => item.PortfolioId).Distinct().ToList();
        var parameterIds = commands.Select(item => item.ParameterId).Distinct().ToList();
        var stored = await db.PortfolioMainProductMonthlyTargets
            .Where(item => portfolioIds.Contains(item.PortfolioId)
                && parameterIds.Contains(item.MainProductParameterId))
            .ToDictionaryAsync(
                item => (item.PortfolioId, item.MainProductParameterId, item.Month),
                cancellationToken);
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Where(item => parameterIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var command in commands)
        {
            var parameter = parameters[command.ParameterId];
            foreach (var month in command.Months)
            {
                var key = (command.PortfolioId, command.ParameterId, month.Month);
                if (stored.TryGetValue(key, out var target))
                {
                    target.TargetValue = Round(month.TargetValue);
                    target.UpdatedAt = now;
                }
                else
                {
                    var created = new PortfolioMainProductMonthlyTarget
                    {
                        PortfolioId = command.PortfolioId,
                        GroupId = parameter.GroupId,
                        MainProductParameterId = command.ParameterId,
                        Month = month.Month,
                        TargetValue = Round(month.TargetValue),
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    db.PortfolioMainProductMonthlyTargets.Add(created);
                    stored[key] = created;
                }
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            Action = "ImportPortfolioMainProductTargets",
            EntityName = nameof(PortfolioMainProductMonthlyTarget),
            EntityKey = token,
            Description =
                $"Excel hedef içe aktarma tamamlandı; bağlam={commands.Count}, aylık hedef={commands.Sum(item => item.Months.Count)}.",
            Actor = actor,
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<List<TargetRowViewModel>> BuildRowsAsync(
        TargetQuery query,
        CancellationToken cancellationToken)
    {
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Include(item => item.Group)
            .Include(item => item.MainProductInstance).ThenInclude(item => item.MainProduct)
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var portfolios = await db.Portfolios.AsNoTracking()
            .Include(item => item.Branch)
            .Include(item => item.ProductGamut)
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var assignments = await db.ProductGamutMainProductAssignments.AsNoTracking()
            .ToListAsync(cancellationToken);
        var exclusions = await db.BranchMainProductExclusions.AsNoTracking()
            .ToListAsync(cancellationToken);
        var targets = await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
            .ToListAsync(cancellationToken);
        var targetMap = targets
            .GroupBy(item => (item.PortfolioId, item.MainProductParameterId))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Values = group.Select(item => item.TargetValue).ToList(),
                    Count = group.Select(item => item.Month).Distinct().Count()
                });

        var rows = new List<TargetRowViewModel>();
        foreach (var portfolio in portfolios)
        {
            foreach (var parameter in parameters.Where(
                         item => item.GroupId == portfolio.GroupId))
            {
                var instance = parameter.MainProductInstance;
                if (!assignments.Any(item =>
                        item.ProductGamutId == portfolio.ProductGamutId
                        && item.MainProductId == instance.MainProductId
                        && IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm,
                            item.EffectiveToYear, item.EffectiveToTerm,
                            instance.Year, instance.Term)))
                {
                    continue;
                }

                if (exclusions.Any(item =>
                        item.BranchId == portfolio.BranchId
                        && item.MainProductId == instance.MainProductId
                        && IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm,
                            item.EffectiveToYear, item.EffectiveToTerm,
                            instance.Year, instance.Term)))
                {
                    continue;
                }

                targetMap.TryGetValue((portfolio.Id, parameter.Id), out var target);
                rows.Add(new TargetRowViewModel
                {
                    PortfolioId = portfolio.Id,
                    ParameterId = parameter.Id,
                    MainProductId = instance.MainProductId,
                    ProductGamutId = portfolio.ProductGamutId,
                    BranchId = portfolio.BranchId,
                    GroupId = portfolio.GroupId,
                    Year = instance.Year,
                    Term = instance.Term,
                    GroupNo = parameter.Group.GroupNo,
                    GroupName = parameter.Group.Name,
                    BranchCode = portfolio.Branch.BranchCode,
                    BranchName = portfolio.Branch.Name,
                    PortfolioCode = portfolio.Code,
                    PortfolioName = portfolio.Name,
                    ProductGamutCode = portfolio.ProductGamut.Code,
                    ProductGamutName = portfolio.ProductGamut.Name,
                    MainProductCode = instance.MainProduct.Code,
                    MainProductName = instance.MainProduct.Name,
                    CalculationType = parameter.CalculationType,
                    PeriodTarget = target is null
                        ? 0
                        : TargetPeriodValueConverter.Aggregate(
                            target.Values, parameter.CalculationType),
                    EnteredMonthCount = target?.Count ?? 0
                });
            }
        }

        rows = ApplyFilters(rows, query);
        return SortRows(rows, query.SortKey, query.SortDirection);
    }

    private static List<TargetRowViewModel> ApplyFilters(
        List<TargetRowViewModel> rows,
        TargetQuery query)
    {
        if (query.GroupId.HasValue)
            rows = rows.Where(row => row.GroupId == query.GroupId.Value).ToList();
        if (query.BranchId.HasValue)
            rows = rows.Where(row => row.BranchId == query.BranchId.Value).ToList();
        if (query.ProductGamutId.HasValue)
            rows = rows.Where(row => row.ProductGamutId == query.ProductGamutId.Value).ToList();
        if (query.PortfolioId.HasValue)
            rows = rows.Where(row => row.PortfolioId == query.PortfolioId.Value).ToList();
        if (query.MainProductId.HasValue)
            rows = rows.Where(row => row.MainProductId == query.MainProductId.Value).ToList();
        if (query.Year.HasValue)
            rows = rows.Where(row => row.Year == query.Year.Value).ToList();
        if (query.Term is 1 or 2)
            rows = rows.Where(row => row.Term == query.Term.Value).ToList();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(row => ContainsTurkish(
                $"{row.GroupNo} {row.GroupName} {row.BranchCode} {row.BranchName} " +
                $"{row.PortfolioCode} {row.PortfolioName} {row.ProductGamutCode} " +
                $"{row.ProductGamutName} {row.MainProductCode} {row.MainProductName}",
                search)).ToList();
        }
        return rows;
    }

    private static List<TargetRowViewModel> SortRows(
        List<TargetRowViewModel> rows,
        string? sortKey,
        string? direction)
    {
        var comparer = StringComparer.Create(TurkishCulture, true);
        var desc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortKey?.Trim().ToLowerInvariant(), desc) switch
        {
            ("term", false) => rows.OrderBy(row => row.Term).ThenBy(row => row.Year).ToList(),
            ("term", true) => rows.OrderByDescending(row => row.Term).ThenByDescending(row => row.Year).ToList(),
            ("group", false) => rows.OrderBy(row => row.GroupNo, comparer).ToList(),
            ("group", true) => rows.OrderByDescending(row => row.GroupNo, comparer).ToList(),
            ("branch", false) => rows.OrderBy(row => row.BranchCode, comparer).ToList(),
            ("branch", true) => rows.OrderByDescending(row => row.BranchCode, comparer).ToList(),
            ("portfolio", false) => rows.OrderBy(row => row.PortfolioCode, comparer).ToList(),
            ("portfolio", true) => rows.OrderByDescending(row => row.PortfolioCode, comparer).ToList(),
            ("gamut", false) => rows.OrderBy(row => row.ProductGamutCode, comparer).ToList(),
            ("gamut", true) => rows.OrderByDescending(row => row.ProductGamutCode, comparer).ToList(),
            ("product", false) => rows.OrderBy(row => row.MainProductCode, comparer).ToList(),
            ("product", true) => rows.OrderByDescending(row => row.MainProductCode, comparer).ToList(),
            ("calculation", false) => rows.OrderBy(row => row.CalculationType.ToString(), comparer).ToList(),
            ("calculation", true) => rows.OrderByDescending(row => row.CalculationType.ToString(), comparer).ToList(),
            ("target", false) => rows.OrderBy(row => row.PeriodTarget).ToList(),
            ("target", true) => rows.OrderByDescending(row => row.PeriodTarget).ToList(),
            ("status", false) => rows.OrderBy(row => row.EnteredMonthCount).ToList(),
            ("status", true) => rows.OrderByDescending(row => row.EnteredMonthCount).ToList(),
            (_, false) => rows.OrderBy(row => row.Year)
                .ThenBy(row => row.Term)
                .ThenBy(row => row.PortfolioCode, comparer).ToList(),
            _ => rows.OrderByDescending(row => row.Year)
                .ThenByDescending(row => row.Term)
                .ThenBy(row => row.PortfolioCode, comparer).ToList()
        };
    }

    private async Task<TargetContext> ValidateContextAsync(
        int parameterId,
        int portfolioId,
        CancellationToken cancellationToken)
    {
        var parameter = await db.MainProductParameters.AsNoTracking()
            .Include(item => item.MainProductInstance).ThenInclude(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == parameterId && item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Aktif ana ürün parametresi bulunamadı.");
        var portfolio = await db.Portfolios.AsNoTracking()
            .Include(item => item.Branch)
            .Include(item => item.ProductGamut)
            .FirstOrDefaultAsync(item => item.Id == portfolioId && item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Aktif portföy bulunamadı.");
        if (portfolio.GroupId != parameter.GroupId)
            throw new InvalidOperationException(
                "Portföy ve ana ürün parametresi aynı gruba ait olmalıdır.");
        var instance = parameter.MainProductInstance;
        var assigned = (await db.ProductGamutMainProductAssignments.AsNoTracking()
                .Where(item => item.ProductGamutId == portfolio.ProductGamutId
                    && item.MainProductId == instance.MainProductId)
                .ToListAsync(cancellationToken))
            .Any(item => IsEffective(
                item.EffectiveFromYear, item.EffectiveFromTerm,
                item.EffectiveToYear, item.EffectiveToTerm,
                instance.Year, instance.Term));
        if (!assigned)
            throw new InvalidOperationException(
                "Ana ürün seçili dönemde portföyün ürün gamına bağlı değil.");
        var excluded = (await db.BranchMainProductExclusions.AsNoTracking()
                .Where(item => item.BranchId == portfolio.BranchId
                    && item.MainProductId == instance.MainProductId)
                .ToListAsync(cancellationToken))
            .Any(item => IsEffective(
                item.EffectiveFromYear, item.EffectiveFromTerm,
                item.EffectiveToYear, item.EffectiveToTerm,
                instance.Year, instance.Term));
        if (excluded)
            throw new InvalidOperationException(
                "Ana ürün seçili dönemde bu şubeden çıkarılmış.");
        return new TargetContext(parameter, portfolio);
    }

    private async Task UpsertMonthlyValuesAsync(
        TargetContext context,
        IReadOnlyList<MonthlyTargetValue> values,
        string actor,
        string action,
        CancellationToken cancellationToken)
    {
        var stored = await db.PortfolioMainProductMonthlyTargets
            .Where(item => item.PortfolioId == context.Portfolio.Id
                && item.MainProductParameterId == context.Parameter.Id)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        var previousTotal = TargetPeriodValueConverter.Aggregate(
            stored.Values.Select(item => item.TargetValue),
            context.Parameter.CalculationType);
        var now = timeProvider.GetUtcNow();
        foreach (var value in values)
        {
            if (stored.TryGetValue(value.Month, out var target))
            {
                target.TargetValue = Round(value.TargetValue);
                target.UpdatedAt = now;
            }
            else
            {
                db.PortfolioMainProductMonthlyTargets.Add(
                    new PortfolioMainProductMonthlyTarget
                    {
                        PortfolioId = context.Portfolio.Id,
                        GroupId = context.Parameter.GroupId,
                        MainProductParameterId = context.Parameter.Id,
                        Month = value.Month,
                        TargetValue = Round(value.TargetValue),
                        CreatedAt = now,
                        UpdatedAt = now
                    });
            }
        }
        var newTotal = TargetPeriodValueConverter.Aggregate(
            values.Select(item => item.TargetValue),
            context.Parameter.CalculationType);
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = nameof(PortfolioMainProductMonthlyTarget),
            EntityKey = $"{context.Portfolio.Id}:{context.Parameter.Id}",
            Description =
                $"{context.Portfolio.Code} · {context.Parameter.MainProductInstance.MainProduct.Code} hedefi güncellendi; " +
                $"kapsam={context.Parameter.MainProductInstance.Year}/{context.Parameter.MainProductInstance.Term}, " +
                $"önceki={previousTotal:N2}, yeni={newTotal:N2}.",
            Actor = actor,
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<ExportTargetValue> BuildExportValues(
        TargetRowViewModel row,
        TargetEntryMode entryMode,
        IReadOnlyDictionary<int, decimal> monthMap)
    {
        var months = TargetPeriodValueConverter.GetTermMonths(row.Term);
        return entryMode switch
        {
            TargetEntryMode.SixMonth =>
            [
                new("Dönem", AggregateIfComplete(months, monthMap, row.CalculationType))
            ],
            TargetEntryMode.ThreeMonth =>
            [
                new("İlk 3 Ay", AggregateIfComplete(
                    months.Take(3), monthMap, row.CalculationType)),
                new("İkinci 3 Ay", AggregateIfComplete(
                    months.Skip(3), monthMap, row.CalculationType))
            ],
            TargetEntryMode.Monthly => months.Select(month =>
                new ExportTargetValue(
                    TurkishMonthNames[month],
                    monthMap.TryGetValue(month, out var value) ? value : null)),
            _ => []
        };
    }

    private static decimal? AggregateIfComplete(
        IEnumerable<int> months,
        IReadOnlyDictionary<int, decimal> monthMap,
        MainProductCalculationType calculationType)
    {
        var monthList = months.ToList();
        return monthList.All(monthMap.ContainsKey)
            ? TargetPeriodValueConverter.Aggregate(
                monthList.Select(month => monthMap[month]), calculationType)
            : null;
    }

    private static List<RawImportRow> ReadWorkbook(
        Stream stream,
        List<string> errors)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault(
            item => string.Equals(item.Name, "Hedefler", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.FirstOrDefault();
        if (sheet is null)
        {
            errors.Add("Excel dosyasında çalışma sayfası bulunamadı.");
            return [];
        }

        var expected = new[]
        {
            "Yıl", "Dönem", "Grup Kodu", "Şube Kodu", "Portföy Kodu",
            "Ana Ürün Kodu", "Giriş Tipi", "Aralık", "Hedef Tutarı"
        };
        for (var column = 1; column <= expected.Length; column++)
        {
            if (!string.Equals(sheet.Cell(1, column).GetString().Trim(),
                    expected[column - 1], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"Excel başlığı geçersiz. {column}. kolon '{expected[column - 1]}' olmalıdır.");
            }
        }
        if (errors.Count > 0)
        {
            return [];
        }

        var result = new List<RawImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var cells = Enumerable.Range(1, expected.Length)
                .Select(column => sheet.Cell(rowNumber, column))
                .ToList();
            if (cells.All(cell => cell.IsEmpty()))
            {
                continue;
            }

            if (!int.TryParse(cells[0].GetString(), out var year)
                || year is < 2000 or > 2100)
            {
                errors.Add($"{rowNumber}. satır: Yıl geçersiz.");
                continue;
            }
            if (!int.TryParse(cells[1].GetString(), out var term)
                || term is not (1 or 2))
            {
                errors.Add($"{rowNumber}. satır: Dönem 1 veya 2 olmalıdır.");
                continue;
            }
            if (!TryReadDecimal(cells[8], out var targetValue)
                || targetValue < 0)
            {
                errors.Add($"{rowNumber}. satır: Hedef tutarı geçersiz.");
                continue;
            }
            if (!TryParseEntryMode(cells[6].GetString(), out var entryMode))
            {
                errors.Add($"{rowNumber}. satır: Giriş tipi geçersiz.");
                continue;
            }

            var groupCode = cells[2].GetString().Trim();
            var branchCode = cells[3].GetString().Trim();
            var portfolioCode = cells[4].GetString().Trim();
            var productCode = cells[5].GetString().Trim();
            var range = cells[7].GetString().Trim();
            if (new[] { groupCode, branchCode, portfolioCode, productCode, range }
                .Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"{rowNumber}. satır: Kod ve aralık alanları boş bırakılamaz.");
                continue;
            }
            result.Add(new RawImportRow(
                rowNumber,
                year,
                term,
                groupCode,
                branchCode,
                portfolioCode,
                productCode,
                entryMode,
                range,
                Round(targetValue)));
        }

        if (result.Count == 0 && errors.Count == 0)
        {
            errors.Add("Excel dosyasında hedef satırı bulunamadı.");
        }
        return result;
    }

    private async Task<List<ResolvedImportCommand>> ResolveImportCommandsAsync(
        IReadOnlyList<RawImportRow> rows,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var groups = await db.GroupDefinitions.AsNoTracking()
            .ToDictionaryAsync(item => item.GroupNo, StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var portfolios = await db.Portfolios.AsNoTracking()
            .Include(item => item.Branch)
            .Include(item => item.ProductGamut)
            .ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var products = await db.ProductDefinitions.AsNoTracking()
            .Where(item => item.Type == ProductType.Main)
            .ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var instances = await db.MainProductInstances.AsNoTracking()
            .ToListAsync(cancellationToken);
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var assignments = await db.ProductGamutMainProductAssignments.AsNoTracking()
            .ToListAsync(cancellationToken);
        var exclusions = await db.BranchMainProductExclusions.AsNoTracking()
            .ToListAsync(cancellationToken);

        var commands = new List<ResolvedImportCommand>();
        var contextGroups = rows.GroupBy(row => new
        {
            Group = row.GroupCode.ToUpperInvariant(),
            Branch = row.BranchCode.ToUpperInvariant(),
            Portfolio = row.PortfolioCode.ToUpperInvariant(),
            Product = row.ProductCode.ToUpperInvariant(),
            row.Year,
            row.Term
        });
        foreach (var contextRows in contextGroups)
        {
            var items = contextRows.ToList();
            if (items.Select(item => item.EntryMode).Distinct().Count() != 1)
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır ve devamı: Aynı hedef bağlamında farklı giriş tipleri kullanılamaz.");
                continue;
            }
            if (items.GroupBy(item => NormalizeRange(item.Range))
                .Any(group => group.Count() > 1))
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır ve devamı: Aynı hedef aralığı birden fazla kez girilmiş.");
                continue;
            }
            groups.TryGetValue(items[0].GroupCode, out var group);
            portfolios.TryGetValue(items[0].PortfolioCode, out var portfolio);
            products.TryGetValue(items[0].ProductCode, out var product);
            if (group is null || portfolio is null || product is null)
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır: Grup, portföy veya ana ürün kodu bulunamadı.");
                continue;
            }
            if (!string.Equals(portfolio.Branch.BranchCode, items[0].BranchCode,
                    StringComparison.OrdinalIgnoreCase)
                || portfolio.GroupId != group.Id)
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır: Portföy, şube ve grup ilişkisi uyumsuz.");
                continue;
            }
            var instance = instances.FirstOrDefault(item =>
                item.MainProductId == product.Id
                && item.Year == items[0].Year
                && item.Term == items[0].Term);
            var parameter = instance is null
                ? null
                : parameters.FirstOrDefault(item =>
                    item.GroupId == group.Id
                    && item.MainProductInstanceId == instance.Id);
            if (instance is null || parameter is null)
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır: Seçili grup, ürün ve dönem için aktif parametre bulunamadı.");
                continue;
            }
            if (!assignments.Any(item =>
                    item.ProductGamutId == portfolio.ProductGamutId
                    && item.MainProductId == product.Id
                    && IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm,
                        item.EffectiveToYear, item.EffectiveToTerm,
                        instance.Year, instance.Term)))
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır: Ürün seçili dönemde portföyün ürün gamında değil.");
                continue;
            }
            if (exclusions.Any(item =>
                    item.BranchId == portfolio.BranchId
                    && item.MainProductId == product.Id
                    && IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm,
                        item.EffectiveToYear, item.EffectiveToTerm,
                        instance.Year, instance.Term)))
            {
                errors.Add(
                    $"{items[0].RowNumber}. satır: Ürün seçili dönemde şubeden çıkarılmış.");
                continue;
            }

            try
            {
                var expanded = ExpandImportRows(
                    instance.Term, parameter.CalculationType, items);
                commands.Add(new ResolvedImportCommand(
                    parameter.Id,
                    portfolio.Id,
                    parameter.CalculationType,
                    expanded,
                    $"{group.GroupNo} · {portfolio.Branch.BranchCode} · {portfolio.Code} · {product.Code} · {instance.Year}/{instance.Term}",
                    EntryModeLabel(items[0].EntryMode)));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"{items[0].RowNumber}. satır ve devamı: {ex.Message}");
            }
        }
        return commands;
    }

    private static IReadOnlyList<MonthlyTargetValue> ExpandImportRows(
        int term,
        MainProductCalculationType calculationType,
        IReadOnlyList<RawImportRow> rows)
    {
        var mode = rows[0].EntryMode;
        decimal? sixMonth = null;
        decimal? firstThree = null;
        decimal? secondThree = null;
        var monthly = new List<MonthlyTargetValue>();
        switch (mode)
        {
            case TargetEntryMode.SixMonth:
                if (rows.Count != 1 || NormalizeRange(rows[0].Range) != "DONEM")
                    throw new InvalidOperationException(
                        "6 aylık girişte yalnız 'Dönem' aralığı bulunmalıdır.");
                sixMonth = rows[0].TargetValue;
                break;
            case TargetEntryMode.ThreeMonth:
                if (rows.Count != 2)
                    throw new InvalidOperationException(
                        "3 aylık girişte ilk ve ikinci üç aylık blok birlikte bulunmalıdır.");
                firstThree = rows.FirstOrDefault(item =>
                    NormalizeRange(item.Range) == "ILK3AY")?.TargetValue;
                secondThree = rows.FirstOrDefault(item =>
                    NormalizeRange(item.Range) == "IKINCI3AY")?.TargetValue;
                if (!firstThree.HasValue || !secondThree.HasValue)
                    throw new InvalidOperationException(
                        "Aralıklar 'İlk 3 Ay' ve 'İkinci 3 Ay' olmalıdır.");
                break;
            case TargetEntryMode.Monthly:
                var termMonths = TargetPeriodValueConverter.GetTermMonths(term);
                foreach (var row in rows)
                {
                    var month = ParseMonth(row.Range);
                    if (!month.HasValue || !termMonths.Contains(month.Value))
                        throw new InvalidOperationException(
                            $"'{row.Range}' seçili dönem için geçerli bir ay değil.");
                    monthly.Add(new MonthlyTargetValue(month.Value, row.TargetValue));
                }
                break;
        }
        return TargetPeriodValueConverter.Expand(
            term,
            calculationType,
            mode,
            sixMonth,
            firstThree,
            secondThree,
            monthly);
    }

    private async Task ValidateStoredCommandsAsync(
        IReadOnlyList<TargetImportCommand> commands,
        CancellationToken cancellationToken)
    {
        foreach (var command in commands)
        {
            _ = await ValidateContextAsync(
                command.ParameterId, command.PortfolioId, cancellationToken);
        }
    }

    private static bool TryReadDecimal(IXLCell cell, out decimal value)
    {
        if (cell.TryGetValue(out double number))
        {
            value = (decimal)number;
            return true;
        }
        var text = cell.GetString().Trim();
        return decimal.TryParse(text, NumberStyles.Number, TurkishCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number,
                CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseEntryMode(
        string value,
        out TargetEntryMode entryMode)
    {
        var normalized = NormalizeRange(value);
        entryMode = normalized switch
        {
            "6AYLIK" or "SIXMONTH" => TargetEntryMode.SixMonth,
            "3AYLIK" or "THREEMONTH" => TargetEntryMode.ThreeMonth,
            "AYLIK" or "MONTHLY" => TargetEntryMode.Monthly,
            _ => default
        };
        return normalized is "6AYLIK" or "SIXMONTH"
            or "3AYLIK" or "THREEMONTH"
            or "AYLIK" or "MONTHLY";
    }

    private static int? ParseMonth(string value)
    {
        if (int.TryParse(value, out var number) && number is >= 1 and <= 12)
        {
            return number;
        }
        var normalized = NormalizeRange(value);
        for (var month = 1; month <= 12; month++)
        {
            if (NormalizeRange(TurkishMonthNames[month]) == normalized)
            {
                return month;
            }
        }
        return null;
    }

    private static string NormalizeRange(string value)
    {
        var upper = value.Trim().ToUpper(TurkishCulture);
        return string.Concat(upper.Normalize(NormalizationForm.FormD)
            .Where(character =>
                CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark
                && !char.IsWhiteSpace(character)
                && character != '.'));
    }

    private static string EntryModeLabel(TargetEntryMode mode) => mode switch
    {
        TargetEntryMode.SixMonth => "6 Aylık",
        TargetEntryMode.ThreeMonth => "3 Aylık",
        TargetEntryMode.Monthly => "Aylık",
        _ => mode.ToString()
    };

    private static bool IsEffective(
        int fromYear,
        int fromTerm,
        int? toYear,
        int? toTerm,
        int year,
        int term)
    {
        var key = year * 10 + term;
        var from = fromYear * 10 + fromTerm;
        var to = toYear.HasValue && toTerm.HasValue
            ? toYear.Value * 10 + toTerm.Value
            : int.MaxValue;
        return key >= from && key <= to;
    }

    private static bool ContainsTurkish(string source, string search) =>
        TurkishCompare.IndexOf(
            source,
            search,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record TargetContext(
        MainProductParameter Parameter,
        Portfolio Portfolio);

    private sealed record ExportTargetValue(string Range, decimal? Value)
    {
        public bool HasValue => Value.HasValue;
    }

    private sealed record RawImportRow(
        int RowNumber,
        int Year,
        int Term,
        string GroupCode,
        string BranchCode,
        string PortfolioCode,
        string ProductCode,
        TargetEntryMode EntryMode,
        string Range,
        decimal TargetValue);

    private sealed record ResolvedImportCommand(
        int ParameterId,
        int PortfolioId,
        MainProductCalculationType CalculationType,
        IReadOnlyList<MonthlyTargetValue> Months,
        string Context,
        string EntryMode);
}
