using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Cliente")]
    [Route("cliente")]
    public class ClienteController : Controller
    {
        private readonly TallerMecanicoContext _db;
        public ClienteController(TallerMecanicoContext db) => _db = db;

        [HttpGet("panel")]
        public IActionResult Panel()
        {
            ViewBag.Nombre = User.Identity?.Name ?? "Cliente";
            ViewBag.ClienteId = User.FindFirst("ClienteId")?.Value ?? "";
            return View();
        }
        [HttpGet("Historial")]
        public async Task<IActionResult> Historial()
        {
            int clienteId = 0;

            if (!int.TryParse(User.FindFirst("ClienteId")?.Value, out clienteId) || clienteId <= 0)
            {
                if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var usuarioId))
                {
                    clienteId = await _db.usuario
                        .Where(u => u.id == usuarioId && u.cliente_id != null)
                        .Select(u => u.cliente_id!.Value)
                        .FirstOrDefaultAsync();
                }
            }

            if (clienteId <= 0) return Unauthorized();
            var data = await _db.atencion
                .AsNoTracking()
                .Where(t => t.cliente_id == clienteId)
                .OrderByDescending(t => t.fecha_ingreso)
                .Select(t => new AtencionHistRow
                {
                    AtencionId = t.id,
                    AgendaId = t.agenda_id,
                    FechaHora = t.fecha_ingreso,
                    Estado = t.estado,
                    Mecanico = t.mecanico != null ? t.mecanico.nombre : null,
                    Taller = t.taller != null ? t.taller.razon_social : null,
                    Vehiculo = t.vehiculo != null ? t.vehiculo.patente : null,
                    Observaciones = t.observaciones
                })
                .Take(200)
                .ToListAsync();
            return View(data);
        }

        [HttpGet("Atencion/{id}")]
        public async Task<IActionResult> Atencion(int id)
        {
            int clienteId = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");

            if (clienteId <= 0)
            {
                return Unauthorized();
            }

            var atencion = await _db.atencion
                .AsNoTracking()
                .Include(a => a.cliente)
                .Include(a => a.vehiculo)
                    .ThenInclude(v => v.tipo)
                .Include(a => a.taller)
                .Include(a => a.mecanico)
                .Include(a => a.administrativo)
                .Include(a => a.agenda)
                .Include(a => a.servicios)
                    .ThenInclude(s => s.tipo_servicio)
                .Include(a => a.servicios)
                    .ThenInclude(s => s.servicio_repuestos)
                        .ThenInclude(sr => sr.repuesto_unidades)
                            .ThenInclude(ru => ru.repuesto)
                .FirstOrDefaultAsync(a => a.id == id && a.cliente_id == clienteId);

            if (atencion == null)
            {
                return NotFound();
            }

            var bitacoras = await _db.bitacora
                .Where(b => b.atencion_id == id)
                .OrderByDescending(b => b.created_at)
                .ToListAsync();

            ViewBag.Bitacoras = bitacoras;

            return View(atencion);
        }

        [HttpPost("Atencion/{id}/AgregarComentario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarComentario(int id, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                return BadRequest("Comentario requerido.");
            }

            int clienteId = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var atencion = await _db.atencion.FindAsync(id);
            if (atencion == null || atencion.cliente_id != clienteId)
            {
                return NotFound();
            }

            var nuevaBitacora = new bitacora
            {
                atencion_id = id,
                descripcion = comentario,
                created_at = DateTime.Now,
                tipo = "Cliente"
            };

            _db.bitacora.Add(nuevaBitacora);
            await _db.SaveChangesAsync();

            return RedirectToAction("Atencion", new { id });
        }


        [HttpGet("Agenda")]
        public IActionResult Agenda()
        {
            return View();
        }

        [HttpGet("GetAgendasCliente")]
        public IActionResult GetAgendasCliente(DateTime start, DateTime end)
        {
            var eventos = _db.agenda
                .Where(a => a.fecha_agenda >= start && a.fecha_agenda <= end)
                .Select(e => new
                {
                    id = e.id,
                    title = "Ocupado",
                    start = e.fecha_agenda.ToString("o"),
                    end = e.fecha_agenda.AddHours(1).ToString("o"),
                })
                .ToList();

            return Json(eventos);
        }

        [HttpGet("CreateAgenda")]
        public async Task<IActionResult> CreateAgenda(string? fecha = null)
        {
            int clienteId = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");

            if (clienteId <= 0)
            {
                return Unauthorized();
            }

            var vehiculos = await _db.cliente_vehiculo
                .Where(cv => cv.cliente_id == clienteId)
                .Select(cv => new SelectListItem
                {
                    Value = cv.vehiculo_id.ToString(),
                    Text = cv.vehiculo.patente
                })
                .ToListAsync();

            var viewModel = new CrearAgendaClienteViewModel
            {
                VehiculosDisponibles = new SelectList(vehiculos, "Value", "Text"),
                FechaAgenda = DateTime.Today.AddHours(9)
            };

            if (!string.IsNullOrEmpty(fecha) && DateTime.TryParse(fecha, out DateTime parsedFecha))
            {
                viewModel.FechaAgenda = parsedFecha;
            }

            return View(viewModel);
        }

        [HttpPost("CreateAgenda")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAgenda(CrearAgendaClienteViewModel viewModel)
        {
            int clienteId = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (clienteId <= 0)
            {
                return Unauthorized();
            }

            var vehiculos = await _db.cliente_vehiculo
                .Where(cv => cv.cliente_id == clienteId)
                .Select(cv => new SelectListItem
                {
                    Value = cv.vehiculo_id.ToString(),
                    Text = cv.vehiculo.patente
                })
                .ToListAsync();

            viewModel.VehiculosDisponibles = new SelectList(vehiculos, "Value", "Text", viewModel.VehiculoId);

            if (ModelState.IsValid)
            {
                var nuevaAgenda = new agenda
                {
                    titulo = "Solicitud de Cliente",
                    fecha_agenda = viewModel.FechaAgenda,
                    comentarios = viewModel.Comentarios,
                    estado = "Pendiente"
                };

                var nuevaAtencion = new atencion
                {
                    observaciones = viewModel.Observaciones,
                    cliente_id = clienteId,
                    vehiculo_id = viewModel.VehiculoId,
                    taller_id = 1,
                    agenda = nuevaAgenda,
                    estado = "Solicitud pendiente"
                };

                _db.atencion.Add(nuevaAtencion);
                await _db.SaveChangesAsync();

                var nuevaBitacora = new bitacora
                {
                    atencion_id = nuevaAtencion.id,
                    descripcion = "Reserva de hora a través del portal.",
                    created_at = DateTime.Now,
                    tipo = "Cliente",
                };

                _db.bitacora.Add(nuevaBitacora);
                await _db.SaveChangesAsync();

                return RedirectToAction("Agenda");
            }

            return View(viewModel);
        }

        [HttpGet("GetVehiculosCliente")]
        public async Task<IActionResult> GetVehiculosCliente()
        {
            int clienteId = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");

            if (clienteId <= 0)
            {
                return Unauthorized();
            }

            var vehiculos = await _db.cliente_vehiculo
                .Where(cv => cv.cliente_id == clienteId)
                .Select(cv => new
                {
                    id = cv.vehiculo.id,
                    texto = cv.vehiculo.patente
                })
                .ToListAsync();

            return Json(vehiculos);
        }

    }
}
