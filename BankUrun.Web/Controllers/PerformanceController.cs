using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class PerformanceController : Controller
{
    [HttpGet]
    public IActionResult Index() => Redirect("/Dashboard");
}
