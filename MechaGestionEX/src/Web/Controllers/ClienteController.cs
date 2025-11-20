using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Authorize(Roles = "Cliente")]
    [Route("cliente")]
    public class ClienteController : Controller
    {
        [HttpGet("panel")]
        public IActionResult Panel()
        {
            ViewBag.Nombre = User.Identity?.Name ?? "Cliente";
            ViewBag.ClienteId = User.FindFirst("ClienteId")?.Value ?? "";
            return View();
        }
    }
}
