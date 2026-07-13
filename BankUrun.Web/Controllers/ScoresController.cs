using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ScoresController : Controller
{
    [HttpGet]
    public IActionResult Index() => Redirect("/Dashboard");
}
