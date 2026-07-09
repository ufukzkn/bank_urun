using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ScoreService(AppDbContext db) : IScoreService
{
    public async Task<ScoreIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.Group.GroupNo)
            .ThenBy(branch => branch.BranchCode)
            .Select(branch => new BranchOptionViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name
            })
            .ToListAsync(cancellationToken);

        var subProductInstances = await db.SubProductInstances
            .AsNoTracking()
            .Include(instance => instance.SubProduct)
            .Include(instance => instance.MainProductInstance)
                .ThenInclude(instance => instance.MainProduct)
            .OrderByDescending(instance => instance.MainProductInstance.Year)
            .ThenByDescending(instance => instance.MainProductInstance.Term)
            .ThenBy(instance => instance.MainProductInstance.MainProduct.Code)
            .ThenBy(instance => instance.SubProduct.Code)
            .Select(instance => new SubProductInstanceOptionViewModel
            {
                Id = instance.Id,
                Year = instance.MainProductInstance.Year,
                Term = instance.MainProductInstance.Term,
                MainProductCode = instance.MainProductInstance.MainProduct.Code,
                MainProductName = instance.MainProductInstance.MainProduct.Name,
                SubProductCode = instance.SubProduct.Code,
                SubProductName = instance.SubProduct.Name
            })
            .ToListAsync(cancellationToken);

        var scores = await db.BranchProductScores
            .AsNoTracking()
            .Include(score => score.Branch)
                .ThenInclude(branch => branch.Group)
            .Include(score => score.SubProductInstance)
                .ThenInclude(instance => instance.SubProduct)
            .Include(score => score.SubProductInstance)
                .ThenInclude(instance => instance.MainProductInstance)
                    .ThenInclude(instance => instance.MainProduct)
            .OrderByDescending(score => score.SubProductInstance.MainProductInstance.Year)
            .ThenByDescending(score => score.SubProductInstance.MainProductInstance.Term)
            .ThenBy(score => score.Branch.Group.GroupNo)
            .ThenBy(score => score.Branch.BranchCode)
            .ThenBy(score => score.SubProductInstance.MainProductInstance.MainProduct.Code)
            .ThenBy(score => score.SubProductInstance.SubProduct.Code)
            .Select(score => new ScoreRowViewModel
            {
                Id = score.Id,
                BranchId = score.BranchId,
                GroupId = score.Branch.GroupId,
                SubProductInstanceId = score.SubProductInstanceId,
                Year = score.SubProductInstance.MainProductInstance.Year,
                Term = score.SubProductInstance.MainProductInstance.Term,
                GroupNo = score.Branch.Group.GroupNo,
                GroupName = score.Branch.Group.Name,
                BranchCode = score.Branch.BranchCode,
                BranchName = score.Branch.Name,
                MainProductCode = score.SubProductInstance.MainProductInstance.MainProduct.Code,
                MainProductName = score.SubProductInstance.MainProductInstance.MainProduct.Name,
                SubProductCode = score.SubProductInstance.SubProduct.Code,
                SubProductName = score.SubProductInstance.SubProduct.Name,
                Score = score.Score,
                TargetValue = score.TargetValue,
                HgoShare = score.HgoShare * 100,
                DevelopmentShare = score.DevelopmentShare * 100,
                SizeShare = score.SizeShare * 100
            })
            .ToListAsync(cancellationToken);

        var branchSuccess = scores
            .GroupBy(score => new { score.BranchId, score.BranchCode, score.BranchName, score.GroupNo })
            .Select(group => new BranchSuccessViewModel
            {
                BranchId = group.Key.BranchId,
                BranchCode = group.Key.BranchCode,
                BranchName = group.Key.BranchName,
                GroupNo = group.Key.GroupNo,
                TotalScore = group.Sum(score => score.Score),
                TotalTarget = group.Sum(score => score.TargetValue)
            })
            .OrderByDescending(item => item.SuccessRate)
            .ThenBy(item => item.BranchCode)
            .ToList();

        return new ScoreIndexViewModel
        {
            Branches = branches,
            SubProductInstances = subProductInstances,
            Scores = scores,
            BranchSuccess = branchSuccess
        };
    }

    public async Task CreateScoreAsync(ScoreInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ValidateScoreInput(input);
        var branch = await db.Branches.Include(item => item.Group).FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var subInstance = await db.SubProductInstances
            .Include(item => item.SubProduct)
            .FirstOrDefaultAsync(item => item.Id == input.SubProductInstanceId, cancellationToken)
            ?? throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");

        if (await db.BranchProductScores.AnyAsync(item => item.BranchId == input.BranchId && item.SubProductInstanceId == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Bu şube ve alt ürün instance için puan satırı zaten var.");
        }

        var now = DateTimeOffset.UtcNow;
        var score = new BranchProductScore
        {
            BranchId = input.BranchId,
            SubProductInstanceId = input.SubProductInstanceId,
            Score = input.Score,
            TargetValue = input.TargetValue,
            HgoShare = ToRatio(input.HgoShare),
            DevelopmentShare = ToRatio(input.DevelopmentShare),
            SizeShare = ToRatio(input.SizeShare),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.BranchProductScores.Add(score);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateBranchProductScore", "BranchProductScore", score.Id.ToString(), $"{branch.BranchCode} - {subInstance.SubProduct.Code} puanı oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateScoreAsync(ScoreInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ValidateScoreInput(input);
        var score = await db.BranchProductScores.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Puan satırı bulunamadı.");

        if (!await db.Branches.AnyAsync(item => item.Id == input.BranchId, cancellationToken))
        {
            throw new InvalidOperationException("Şube bulunamadı.");
        }

        if (!await db.SubProductInstances.AnyAsync(item => item.Id == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");
        }

        if (await db.BranchProductScores.AnyAsync(item => item.Id != input.Id && item.BranchId == input.BranchId && item.SubProductInstanceId == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Bu şube ve alt ürün instance için puan satırı zaten var.");
        }

        score.BranchId = input.BranchId;
        score.SubProductInstanceId = input.SubProductInstanceId;
        score.Score = input.Score;
        score.TargetValue = input.TargetValue;
        score.HgoShare = ToRatio(input.HgoShare);
        score.DevelopmentShare = ToRatio(input.DevelopmentShare);
        score.SizeShare = ToRatio(input.SizeShare);
        score.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("UpdateBranchProductScore", "BranchProductScore", score.Id.ToString(), "Şube puan satırı güncellendi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteScoreAsync(ScoreIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var score = await db.BranchProductScores
            .Include(item => item.Branch)
            .Include(item => item.SubProductInstance)
                .ThenInclude(instance => instance.SubProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Puan satırı bulunamadı.");

        AddAudit("DeleteBranchProductScore", "BranchProductScore", score.Id.ToString(), $"{score.Branch.BranchCode} - {score.SubProductInstance.SubProduct.Code} puanı silindi.", actor);
        db.BranchProductScores.Remove(score);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ValidateScoreInput(ScoreInput input)
    {
        if (input.Score < 0 || input.TargetValue < 0)
        {
            throw new InvalidOperationException("Puan ve hedef negatif olamaz.");
        }

        if (!IsPercent(input.HgoShare) || !IsPercent(input.DevelopmentShare) || !IsPercent(input.SizeShare))
        {
            throw new InvalidOperationException("Pay değerleri 0 ile 100 arasında olmalı.");
        }
    }

    private static bool IsPercent(decimal value)
    {
        return value is >= 0 and <= 100;
    }

    private static decimal ToRatio(decimal percent)
    {
        return percent / 100;
    }

    private void AddAudit(string action, string entityName, string entityKey, string description, string actor)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityKey = entityKey,
            Description = description,
            Actor = string.IsNullOrWhiteSpace(actor) ? "local-user" : actor,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
