using Infrastructure.Migrations;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    public class AgendaController : Controller
    {
        private readonly TallerMecanicoContext _context;

        public AgendaController(TallerMecanicoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAgendas(DateTime start, DateTime end)
        {
            var eventos = _context.agenda
                                  .Where(a => a.fecha_agenda >= start && a.fecha_agenda <= end)
                                  .Select(e => new {
                                      id = e.id,
                                      title = e.titulo,
                                      start = e.fecha_agenda.ToString("o"),
                                      end = e.fecha_agenda.AddHours(1).ToString("o"),
                                      url = Url.Action("detalle", "atencion", new { id = e.id })
                                  })
                                  .ToList();

            return Json(eventos);
        }
    }
}