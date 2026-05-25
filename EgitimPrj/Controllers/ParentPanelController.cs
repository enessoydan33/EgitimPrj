using EgitimPrj.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPrj.Controllers
{
    [ParentAuthorize]
    public class ParentPanelController : Controller
    {
        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction("ParentSummary", "Dashboard");
        }

        [HttpGet]
        public IActionResult Messages()
        {
            ViewData["Title"] = "Mesajlar";
            return View("Homeworks");
        }

        [HttpGet]
        public IActionResult Appointments()
        {
            ViewData["Title"] = "Randevu ve program";
            return View();
        }
    }
}

