using System.ComponentModel.DataAnnotations;

namespace BankUrun.Web.ViewModels;

public class ScoreIndexViewModel
{
    public IReadOnlyList<BranchOptionViewModel> Branches { get; set; } = [];
    public IReadOnlyList<SubProductInstanceOptionViewModel> SubProductInstances { get; set; } = [];
    public IReadOnlyList<ScoreRowViewModel> Scores { get; set; } = [];
    public IReadOnlyList<BranchSuccessViewModel> BranchSuccess { get; set; } = [];
    public decimal TotalScore => Scores.Sum(score => score.Score);
    public decimal TotalTarget => Scores.Sum(score => score.TargetValue);
    public decimal SuccessRate => TotalTarget == 0 ? 0 : TotalScore / TotalTarget * 100;
}

public class BranchOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Label => $"{BranchCode} - {Name} ({GroupNo})";
}

public class SubProductInstanceOptionViewModel
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public string SubProductCode { get; set; } = string.Empty;
    public string SubProductName { get; set; } = string.Empty;
    public string Label => $"{Year}/{Term} {MainProductCode} - {SubProductCode} {SubProductName}";
}

public class ScoreRowViewModel
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int GroupId { get; set; }
    public int SubProductInstanceId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public string SubProductCode { get; set; } = string.Empty;
    public string SubProductName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal DisplayedScore => Score;
    public decimal TargetValue { get; set; }
    public decimal HgoShare { get; set; }
    public decimal DevelopmentShare { get; set; }
    public decimal SizeShare { get; set; }
    public decimal SuccessRate => TargetValue == 0 ? 0 : Score / TargetValue * 100;
}

public class BranchSuccessViewModel
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal TotalTarget { get; set; }
    public decimal SuccessRate => TotalTarget == 0 ? 0 : TotalScore / TotalTarget * 100;
}

public class ScoreInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Range(1, int.MaxValue)]
    public int SubProductInstanceId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Puan negatif olamaz.")]
    public decimal Score { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Hedef negatif olamaz.")]
    public decimal TargetValue { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "HGO payı 0 ile 100 arasında olmalı.")]
    public decimal HgoShare { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "Gelişim payı 0 ile 100 arasında olmalı.")]
    public decimal DevelopmentShare { get; set; }

    [Range(typeof(decimal), "0", "100", ErrorMessage = "Büyüklük payı 0 ile 100 arasında olmalı.")]
    public decimal SizeShare { get; set; }
}

public class ScoreIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
