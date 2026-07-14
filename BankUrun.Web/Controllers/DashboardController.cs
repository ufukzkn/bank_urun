using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => Redirect("/Performance");
}
