using Infrastructure.Migrations;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Web.Models;
using Web.ViewModels;

namespace Web.Controllers
{
    [Authorize(Roles = "Funcionario")]
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
            int? tallerId = HttpContext.Session.GetInt32("TallerId");
            if (!tallerId.HasValue)
            {
                var tallerIdClaim = User.FindFirstValue("TallerId");
                if (!int.TryParse(tallerIdClaim, out int parsedTallerId))
                {
                    return BadRequest("No se pudo obtener el taller asignado.");
                }
                tallerId = parsedTallerId;
            }

            var eventos = _context.atencion
                .Where(at => at.taller_id == tallerId.Value &&
                             at.agenda.fecha_agenda >= start &&
                             at.agenda.fecha_agenda <= end)
                .Select(at => new
                {
                    id = at.id,
                    title = at.agenda.titulo,
                    start = at.agenda.fecha_agenda.ToString("o"),
                    end = at.agenda.fecha_agenda.AddHours(1).ToString("o"),
                    url = Url.Action("detalle", "atencion", new { id = at.id })
                })
                .ToList();

            return Json(eventos);
        }

        public async Task<IActionResult> Create(string fecha)
        {
            var viewModel = new CrearAgendaViewModel
            {
                ClientesDisponibles = new SelectList(await _context.cliente.ToListAsync(), "id", "nombre"),
                FechaAgenda = string.IsNullOrEmpty(fecha)
                    ? DateTime.Today.AddHours(9)
                    : DateTime.Parse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearAgendaViewModel viewModel)
        {
            var funcionarioIdClaim = User.FindFirstValue("FuncionarioId");
            if (string.IsNullOrEmpty(funcionarioIdClaim) || !int.TryParse(funcionarioIdClaim, out int funcionarioId))
            {
                ModelState.AddModelError(string.Empty, "No se pudo identificar al funcionario. Asegúrese de que el claim 'FuncionarioId' esté correctamente asignado e inicie sesión nuevamente.");
                viewModel.ClientesDisponibles = new SelectList(await _context.cliente.ToListAsync(), "id", "nombre", viewModel.ClienteId);
                return View(viewModel);
            }

            var tallerIdClaim = User.FindFirstValue("TallerId");
            var tallerIdSession = HttpContext.Session.GetInt32("TallerId");
            int tallerId = tallerIdSession ?? (int.TryParse(tallerIdClaim, out int parsedTallerId) ? parsedTallerId : 0);

            if (tallerId == 0)
            {
                ModelState.AddModelError(string.Empty, "No se pudo identificar el taller asignado. Asegúrese de que el claim o sesión 'TallerId' esté correctamente configurado.");
                viewModel.ClientesDisponibles = new SelectList(await _context.cliente.ToListAsync(), "id", "nombre", viewModel.ClienteId);
                return View(viewModel);
            }

            if (ModelState.IsValid)
            {
                var nuevaAgenda = new agenda
                {
                    titulo = viewModel.Titulo,
                    fecha_agenda = viewModel.FechaAgenda,
                    estado = "Pendiente"
                };

                var nuevaAtencion = new atencion
                {
                    observaciones = viewModel.Observaciones,
                    cliente_id = viewModel.ClienteId,
                    vehiculo_id = viewModel.VehiculoId,
                    administrativo_id = funcionarioId,
                    taller_id = tallerId,
                    agenda = nuevaAgenda
                };

                _context.atencion.Add(nuevaAtencion);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            viewModel.ClientesDisponibles = new SelectList(await _context.cliente.ToListAsync(), "id", "nombre", viewModel.ClienteId);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehiculosPorCliente(int clienteId)
        {
            var vehiculos = await _context.cliente_vehiculo
                .Where(cv => cv.cliente_id == clienteId)
                .Select(cv => new {
                    id = cv.vehiculo.id,
                    texto = cv.vehiculo.patente
                })
                .ToListAsync();

            return Json(vehiculos);
        }
    }
}