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
            .OrderBy(group => group.GroupNo)
            .Select(group => new GroupRowViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name
            })
            .ToListAsync(cancellationToken);

        var units = await db.UnitDefinitions
            .AsNoTracking()
            .OrderBy(unit => unit.UnitNo)
            .Select(unit => new UnitRowViewModel
            {
                Id = unit.Id,
                UnitNo = unit.UnitNo,
                Name = unit.Name
            })
            .ToListAsync(cancellationToken);

        var branches = await db.Branches
            .AsNoTracking()
            .OrderBy(branch => branch.BranchCode)
            .Select(branch => new BranchRowViewModel
            {
                Id = branch.Id,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                BranchType = branch.BranchType
            })
            .ToListAsync(cancellationToken);

        var groupUnits = await db.GroupUnits
            .AsNoTracking()
            .Include(link => link.Group)
            .Include(link => link.Unit)
            .OrderBy(link => link.Group.GroupNo)
            .ThenBy(link => link.Unit.UnitNo)
            .Select(link => new GroupUnitRowViewModel
            {
                Id = link.Id,
                GroupId = link.GroupId,
                UnitId = link.UnitId,
                GroupNo = link.Group.GroupNo,
                GroupName = link.Group.Name,
                UnitNo = link.Unit.UnitNo,
                UnitName = link.Unit.Name
            })
            .ToListAsync(cancellationToken);

        var branchUnits = await db.BranchUnits
            .AsNoTracking()
            .Include(link => link.Branch)
            .Include(link => link.Unit)
            .OrderBy(link => link.Branch.BranchCode)
            .ThenBy(link => link.Unit.UnitNo)
            .Select(link => new BranchUnitRowViewModel
            {
                Id = link.Id,
                BranchId = link.BranchId,
                UnitId = link.UnitId,
                BranchCode = link.Branch.BranchCode,
                BranchName = link.Branch.Name,
                BranchType = link.Branch.BranchType,
                UnitNo = link.Unit.UnitNo,
                UnitName = link.Unit.Name
            })
            .ToListAsync(cancellationToken);

        return new OrganizationIndexViewModel
        {
            Groups = groups,
            Units = units,
            Branches = branches,
            GroupUnits = groupUnits,
            BranchUnits = branchUnits
        };
    }

    public async Task CreateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var groupNo = NormalizeCode(input.GroupNo);
        await EnsureGroupNoAvailableAsync(groupNo, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var group = new GroupDefinition { GroupNo = groupNo, Name = input.Name.Trim(), CreatedAt = now, UpdatedAt = now };
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
        var old = $"{group.GroupNo} {group.Name}";
        group.GroupNo = groupNo;
        group.Name = input.Name.Trim();
        group.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("UpdateGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{old} -> {group.GroupNo} {group.Name}", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteGroupAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        AddAudit("DeleteGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{group.GroupNo} grubu silindi.", actor);
        db.GroupDefinitions.Remove(group);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CreateUnitAsync(UnitInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var unitNo = NormalizeCode(input.UnitNo);
        await EnsureUnitNoAvailableAsync(unitNo, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var unit = new UnitDefinition { UnitNo = unitNo, Name = input.Name.Trim(), CreatedAt = now, UpdatedAt = now };
        db.UnitDefinitions.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateUnitDefinition", "UnitDefinition", unit.Id.ToString(), $"{unit.UnitNo} birimi oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateUnitAsync(UnitInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var unit = await db.UnitDefinitions.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Birim bulunamadı.");
        var unitNo = NormalizeCode(input.UnitNo);
        await EnsureUnitNoAvailableAsync(unitNo, unit.Id, cancellationToken);
        var old = $"{unit.UnitNo} {unit.Name}";
        unit.UnitNo = unitNo;
        unit.Name = input.Name.Trim();
        unit.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("UpdateUnitDefinition", "UnitDefinition", unit.Id.ToString(), $"{old} -> {unit.UnitNo} {unit.Name}", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteUnitAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var unit = await db.UnitDefinitions.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Birim bulunamadı.");
        AddAudit("DeleteUnitDefinition", "UnitDefinition", unit.Id.ToString(), $"{unit.UnitNo} birimi silindi.", actor);
        db.UnitDefinitions.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CreateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branchCode = NormalizeCode(input.BranchCode);
        await EnsureBranchCodeAvailableAsync(branchCode, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var branch = new Branch { BranchCode = branchCode, Name = input.Name.Trim(), BranchType = input.BranchType, CreatedAt = now, UpdatedAt = now };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateBranch", "Branch", branch.Id.ToString(), $"{branch.BranchCode} şubesi oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var branchCode = NormalizeCode(input.BranchCode);
        await EnsureBranchCodeAvailableAsync(branchCode, branch.Id, cancellationToken);
        var old = $"{branch.BranchCode} {branch.Name} {branch.BranchType}";
        branch.BranchCode = branchCode;
        branch.Name = input.Name.Trim();
        branch.BranchType = input.BranchType;
        branch.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("UpdateBranch", "Branch", branch.Id.ToString(), $"{old} -> {branch.BranchCode} {branch.Name} {branch.BranchType}", actor);
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

    public async Task AddGroupUnitAsync(GroupUnitInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var unit = await db.UnitDefinitions.FirstOrDefaultAsync(item => item.Id == input.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Birim bulunamadı.");
        if (await db.GroupUnits.AnyAsync(item => item.GroupId == input.GroupId && item.UnitId == input.UnitId, cancellationToken))
        {
            throw new InvalidOperationException("Bu grup-birim bağlantısı zaten var.");
        }

        var link = new GroupUnit { GroupId = input.GroupId, UnitId = input.UnitId, CreatedAt = DateTimeOffset.UtcNow };
        db.GroupUnits.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("AddGroupUnit", "GroupUnit", link.Id.ToString(), $"{group.GroupNo} grubuna {unit.UnitNo} birimi bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveGroupUnitAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var link = await db.GroupUnits.Include(item => item.Group).Include(item => item.Unit).FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Grup-birim bağlantısı bulunamadı.");
        AddAudit("RemoveGroupUnit", "GroupUnit", link.Id.ToString(), $"{link.Group.GroupNo} - {link.Unit.UnitNo} bağlantısı kaldırıldı.", actor);
        db.GroupUnits.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddBranchUnitAsync(BranchUnitInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var unit = await db.UnitDefinitions.FirstOrDefaultAsync(item => item.Id == input.UnitId, cancellationToken)
            ?? throw new InvalidOperationException("Birim bulunamadı.");
        if (await db.BranchUnits.AnyAsync(item => item.BranchId == input.BranchId && item.UnitId == input.UnitId, cancellationToken))
        {
            throw new InvalidOperationException("Bu şube-birim bağlantısı zaten var.");
        }

        var link = new BranchUnit { BranchId = input.BranchId, UnitId = input.UnitId, CreatedAt = DateTimeOffset.UtcNow };
        db.BranchUnits.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("AddBranchUnit", "BranchUnit", link.Id.ToString(), $"{branch.BranchCode} şubesine {unit.UnitNo} birimi bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveBranchUnitAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var link = await db.BranchUnits.Include(item => item.Branch).Include(item => item.Unit).FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube-birim bağlantısı bulunamadı.");
        AddAudit("RemoveBranchUnit", "BranchUnit", link.Id.ToString(), $"{link.Branch.BranchCode} - {link.Unit.UnitNo} bağlantısı kaldırıldı.", actor);
        db.BranchUnits.Remove(link);
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

    private async Task EnsureUnitNoAvailableAsync(string unitNo, int? currentId, CancellationToken cancellationToken)
    {
        if (await db.UnitDefinitions.AnyAsync(item => item.UnitNo == unitNo && item.Id != currentId, cancellationToken))
        {
            throw new InvalidOperationException($"{unitNo} numaralı birim zaten var.");
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
