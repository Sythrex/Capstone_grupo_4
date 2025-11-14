using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                // Fallback: obtén el cliente_id desde el usuario autenticado
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


    }
}
