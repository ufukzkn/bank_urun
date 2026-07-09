using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ScoreService(AppDbContext db) : IScoreService
{
    public async Task<ScoreIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions
            .AsNoTracking()
            .OrderBy(group => group.GroupNo)
            .Select(group => new GroupOptionViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name
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

        var scores = await db.GroupProductScores
            .AsNoTracking()
            .Include(score => score.Group)
            .Include(score => score.SubProductInstance)
                .ThenInclude(instance => instance.SubProduct)
            .Include(score => score.SubProductInstance)
                .ThenInclude(instance => instance.MainProductInstance)
                    .ThenInclude(instance => instance.MainProduct)
            .OrderByDescending(score => score.SubProductInstance.MainProductInstance.Year)
            .ThenByDescending(score => score.SubProductInstance.MainProductInstance.Term)
            .ThenBy(score => score.Group.GroupNo)
            .ThenBy(score => score.SubProductInstance.MainProductInstance.MainProduct.Code)
            .ThenBy(score => score.SubProductInstance.SubProduct.Code)
            .Select(score => new ScoreRowViewModel
            {
                Id = score.Id,
                GroupId = score.GroupId,
                SubProductInstanceId = score.SubProductInstanceId,
                Year = score.SubProductInstance.MainProductInstance.Year,
                Term = score.SubProductInstance.MainProductInstance.Term,
                GroupNo = score.Group.GroupNo,
                GroupName = score.Group.Name,
                MainProductCode = score.SubProductInstance.MainProductInstance.MainProduct.Code,
                MainProductName = score.SubProductInstance.MainProductInstance.MainProduct.Name,
                SubProductCode = score.SubProductInstance.SubProduct.Code,
                SubProductName = score.SubProductInstance.SubProduct.Name,
                Score = score.Score,
                TargetValue = score.TargetValue,
                HgoShare = score.HgoShare,
                DevelopmentShare = score.DevelopmentShare,
                SizeShare = score.SizeShare
            })
            .ToListAsync(cancellationToken);

        return new ScoreIndexViewModel
        {
            Groups = groups,
            SubProductInstances = subProductInstances,
            Scores = scores
        };
    }

    public async Task CreateScoreAsync(ScoreInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ValidateScoreInput(input);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var subInstance = await db.SubProductInstances
            .Include(item => item.SubProduct)
            .FirstOrDefaultAsync(item => item.Id == input.SubProductInstanceId, cancellationToken)
            ?? throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");

        if (await db.GroupProductScores.AnyAsync(item => item.GroupId == input.GroupId && item.SubProductInstanceId == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Bu grup ve alt ürün instance için puan satırı zaten var.");
        }

        var now = DateTimeOffset.UtcNow;
        var score = new GroupProductScore
        {
            GroupId = input.GroupId,
            SubProductInstanceId = input.SubProductInstanceId,
            Score = input.Score,
            TargetValue = input.TargetValue,
            HgoShare = input.HgoShare,
            DevelopmentShare = input.DevelopmentShare,
            SizeShare = input.SizeShare,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.GroupProductScores.Add(score);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateGroupProductScore", "GroupProductScore", score.Id.ToString(), $"{group.GroupNo} - {subInstance.SubProduct.Code} puanı oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateScoreAsync(ScoreInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ValidateScoreInput(input);
        var score = await db.GroupProductScores.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Puan satırı bulunamadı.");

        if (!await db.GroupDefinitions.AnyAsync(item => item.Id == input.GroupId, cancellationToken))
        {
            throw new InvalidOperationException("Grup bulunamadı.");
        }

        if (!await db.SubProductInstances.AnyAsync(item => item.Id == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");
        }

        if (await db.GroupProductScores.AnyAsync(item => item.Id != input.Id && item.GroupId == input.GroupId && item.SubProductInstanceId == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Bu grup ve alt ürün instance için puan satırı zaten var.");
        }

        score.GroupId = input.GroupId;
        score.SubProductInstanceId = input.SubProductInstanceId;
        score.Score = input.Score;
        score.TargetValue = input.TargetValue;
        score.HgoShare = input.HgoShare;
        score.DevelopmentShare = input.DevelopmentShare;
        score.SizeShare = input.SizeShare;
        score.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("UpdateGroupProductScore", "GroupProductScore", score.Id.ToString(), "Puan satırı güncellendi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteScoreAsync(ScoreIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var score = await db.GroupProductScores
            .Include(item => item.Group)
            .Include(item => item.SubProductInstance)
                .ThenInclude(instance => instance.SubProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Puan satırı bulunamadı.");

        AddAudit("DeleteGroupProductScore", "GroupProductScore", score.Id.ToString(), $"{score.Group.GroupNo} - {score.SubProductInstance.SubProduct.Code} puanı silindi.", actor);
        db.GroupProductScores.Remove(score);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ValidateScoreInput(ScoreInput input)
    {
        if (input.Score < 0 || input.TargetValue < 0)
        {
            throw new InvalidOperationException("Puan ve hedef negatif olamaz.");
        }

        if (!IsShare(input.HgoShare) || !IsShare(input.DevelopmentShare) || !IsShare(input.SizeShare))
        {
            throw new InvalidOperationException("Pay değerleri 0 ile 1 arasında olmalı.");
        }
    }

    private static bool IsShare(decimal value)
    {
        return value is >= 0 and <= 1;
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
