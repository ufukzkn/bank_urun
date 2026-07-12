using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ScoresController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        TempData["Success"] = "Eski puan ekranı Performans Merkezi'ne taşındı.";
        return RedirectToAction("Index", "Performance");
    }
}
