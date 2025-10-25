using Infrastructure.Migrations;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Web.Models;
using Web.ViewModels;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Create()
        {
            var viewModel = new CrearAgendaViewModel
            {
                ClientesDisponibles = new SelectList(await _context.cliente.ToListAsync(), "id", "nombre"),
                FechaAgenda = DateTime.Today.AddHours(9)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearAgendaViewModel viewModel)
        {
            //var nombreUsuario = User.FindFirstValue(ClaimTypes.Name);
            ////var usuario = await _context.usuario.FirstOrDefaultAsync(u => u.nombre_usuario == nombreUsuario);
            //var administrativoId = usuario?.funcionario_id;

            // usuario hardcodeado
            var usuario = new Infrastructure.Persistence.Models.usuario
            {
                id = 1,
                nombre_usuario = "admin",
                password_hash = null,
                cliente_id = null,
                funcionario_id = 1 
            };

            var administrativoId = usuario.funcionario_id;

            if (administrativoId == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo identificar al funcionario. Inicie sesión nuevamente.");
            }

            if (ModelState.IsValid)
            {
                var nuevaAgenda = new agenda
                {
                    titulo = viewModel.Titulo,
                    fecha_agenda = viewModel.FechaAgenda,
                    comentarios = viewModel.Comentarios,
                    estado = "Pendiente"
                };

                var nuevaAtencion = new atencion
                {
                    observaciones = viewModel.Observaciones,
                    cliente_id = viewModel.ClienteId,
                    vehiculo_id = viewModel.VehiculoId,
                    administrativo_id = administrativoId.Value,
                    taller_id = 1, // modificar para asignar taller logeado
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