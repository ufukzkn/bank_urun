using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class OrganizationService(AppDbContext db) : IOrganizationService
{
    public async Task<OrganizationIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions
            .AsNoTracking()
            .Include(group => group.Branches)
            .OrderBy(group => group.GroupNo)
            .Select(group => new GroupRowViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name,
                GroupSegment = group.GroupSegment,
                IsActive = group.IsActive,
                BranchPerformanceEnabled = group.BranchPerformanceEnabled,
                MiyPerformanceEnabled = group.MiyPerformanceEnabled,
                ScaleEnabled = group.ScaleEnabled,
                BranchCount = group.Branches.Count
            })
            .ToListAsync(cancellationToken);

        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.Group.GroupNo)
            .ThenBy(branch => branch.BranchCode)
            .Select(branch => new BranchRowViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name,
                GroupSegment = branch.Group.GroupSegment,
                BranchCode = branch.BranchCode,
                Name = branch.Name
            })
            .ToListAsync(cancellationToken);

        return new OrganizationIndexViewModel
        {
            Groups = groups,
            Branches = branches
        };
    }

    public async Task CreateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var groupNo = NormalizeCode(input.GroupNo);
        await EnsureGroupNoAvailableAsync(groupNo, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var group = new GroupDefinition
        {
            GroupNo = groupNo,
            Name = input.Name.Trim(),
            GroupSegment = input.GroupSegment,
            IsActive = input.IsActive,
            BranchPerformanceEnabled = input.BranchPerformanceEnabled,
            MiyPerformanceEnabled = input.MiyPerformanceEnabled,
            ScaleEnabled = input.ScaleEnabled,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.GroupDefinitions.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{group.GroupNo} grubu oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var groupNo = NormalizeCode(input.GroupNo);
        await EnsureGroupNoAvailableAsync(groupNo, group.Id, cancellationToken);
        var old = $"{group.GroupNo} {group.Name} {group.GroupSegment}";

        group.GroupNo = groupNo;
        group.Name = input.Name.Trim();
        group.GroupSegment = input.GroupSegment;
        group.IsActive = input.IsActive;
        group.BranchPerformanceEnabled = input.BranchPerformanceEnabled;
        group.MiyPerformanceEnabled = input.MiyPerformanceEnabled;
        group.ScaleEnabled = input.ScaleEnabled;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit("UpdateGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{old} -> {group.GroupNo} {group.Name} {group.GroupSegment}", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteGroupAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions
            .Include(item => item.Branches)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");

        if (group.Branches.Count > 0)
        {
            throw new InvalidOperationException($"{group.GroupNo} grubuna bağlı şubeler var. Önce şubeleri başka gruba taşıyın veya silin.");
        }

        AddAudit("DeleteGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{group.GroupNo} grubu silindi.", actor);
        db.GroupDefinitions.Remove(group);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CreateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Bağlı grup bulunamadı.");
        var branchCode = NormalizeCode(input.BranchCode);
        await EnsureBranchCodeAvailableAsync(branchCode, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var branch = new Branch
        {
            GroupId = input.GroupId,
            BranchCode = branchCode,
            Name = input.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateBranch", "Branch", branch.Id.ToString(), $"{branch.BranchCode} şubesi {group.GroupNo} grubuna bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branch = await db.Branches.Include(item => item.Group).FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Bağlı grup bulunamadı.");
        var branchCode = NormalizeCode(input.BranchCode);
        await EnsureBranchCodeAvailableAsync(branchCode, branch.Id, cancellationToken);
        var old = $"{branch.BranchCode} {branch.Name} ({branch.Group.GroupNo})";

        branch.GroupId = input.GroupId;
        branch.BranchCode = branchCode;
        branch.Name = input.Name.Trim();
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit("UpdateBranch", "Branch", branch.Id.ToString(), $"{old} -> {branch.BranchCode} {branch.Name} ({group.GroupNo})", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteBranchAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");

        AddAudit("DeleteBranch", "Branch", branch.Id.ToString(), $"{branch.BranchCode} şubesi silindi.", actor);
        db.Branches.Remove(branch);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureGroupNoAvailableAsync(string groupNo, int? currentId, CancellationToken cancellationToken)
    {
        if (await db.GroupDefinitions.AnyAsync(item => item.GroupNo == groupNo && item.Id != currentId, cancellationToken))
        {
            throw new InvalidOperationException($"{groupNo} numaralı grup zaten var.");
        }
    }

    private async Task EnsureBranchCodeAvailableAsync(string branchCode, int? currentId, CancellationToken cancellationToken)
    {
        if (await db.Branches.AnyAsync(item => item.BranchCode == branchCode && item.Id != currentId, cancellationToken))
        {
            throw new InvalidOperationException($"{branchCode} kodlu şube zaten var.");
        }
    }

    private static string NormalizeCode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Kod/no boş olamaz.");
        }

        return normalized;
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
