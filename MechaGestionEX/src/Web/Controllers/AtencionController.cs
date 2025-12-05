using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Funcionario")]
    public class AtencionController : Controller
    {
        private readonly TallerMecanicoContext _context;

        public AtencionController(TallerMecanicoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var atencion = await _context.atencion
                .Include(a => a.agenda)
                .Include(a => a.cliente)
                .Include(a => a.vehiculo)
                    .ThenInclude(v => v.tipo)
                .Include(a => a.taller)
                .Include(a => a.mecanico)
                .Include(a => a.administrativo)
                .Include(a => a.servicios)
                    .ThenInclude(s => s.tipo_servicio)
                .Include(a => a.servicios)
                    .ThenInclude(s => s.servicio_repuestos)
                        .ThenInclude(sr => sr.repuesto_unidades)
                            .ThenInclude(ru => ru.repuesto)
                .FirstOrDefaultAsync(a => a.agenda_id == id-1);

            if (atencion == null)
            {
                return NotFound();
            }

            var bitacoras = await _context.bitacora
                .Where(b => b.atencion_id == atencion.id)
                .OrderByDescending(b => b.created_at)
                .ToListAsync();

            ViewBag.Bitacoras = bitacoras;

            return View(atencion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarBitacora(int id, string descripcion, string tipo, IFormFile? imagen)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                return BadRequest("Descripción requerida.");
            }

            var atencion = await _context.atencion.FindAsync(id);
            if (atencion == null)
            {
                return NotFound();
            }

            byte[]? imagenBytes = null;
            if (imagen != null && imagen.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imagen.CopyToAsync(memoryStream);
                    imagenBytes = memoryStream.ToArray();
                }
            }

            var nuevaBitacora = new bitacora
            {
                atencion_id = id,
                descripcion = descripcion,
                created_at = DateTime.Now,
                tipo = tipo,
                imagen = imagenBytes
            };

            _context.bitacora.Add(nuevaBitacora);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detalle", new { id = atencion.agenda_id + 1 });
        }

        public async Task<IActionResult> Editar_Atencion(int id)
        {
            var atencion = await _context.atencion
                .Include(a => a.servicios)
                    .ThenInclude(s => s.tipo_servicio)
                .Include(a => a.servicios)
                    .ThenInclude(s => s.servicio_repuestos)
                        .ThenInclude(sr => sr.repuesto_unidades)
                            .ThenInclude(ru => ru.repuesto)
                .FirstOrDefaultAsync(a => a.id == id);
            if (atencion == null)
            {
                return NotFound();
            }
            return View(atencion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("id,kilometraje_ingreso,observaciones,estado,taller_id,vehiculo_id,mecanico_id,administrativo_id,cliente_id,cotizacion_id,agenda_id,fecha_ingreso")] atencion atencion)
        {
            if (id != atencion.id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(atencion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.atencion.Any(e => e.id == atencion.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Detalle), new { id = atencion.agenda_id });
            }
            return View(atencion);
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var atencion = await _context.atencion
                .Include(a => a.agenda)
                .Include(a => a.servicios)
                    .ThenInclude(s => s.servicio_repuestos)
                .FirstOrDefaultAsync(a => a.id == id);
            if (atencion == null)
            {
                return NotFound();
            }

            try
            {
                foreach (var servicio in atencion.servicios)
                {
                    _context.servicio_repuesto.RemoveRange(servicio.servicio_repuestos);
                    _context.servicio.Remove(servicio);
                }

                if (atencion.agenda != null)
                {
                    _context.agenda.Remove(atencion.agenda);
                }
                _context.atencion.Remove(atencion);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "AgendaController");
            }
            catch (DbUpdateException)
            {
                return RedirectToAction(nameof(Detalle), new { id = atencion.agenda_id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTiposServicio(string search = null)
        {
            var tipos = await _context.tipo_servicio
                .Where(ts => string.IsNullOrEmpty(search) || ts.nombre.Contains(search))
                .Select(ts => new { id = ts.id, text = ts.nombre })
                .ToListAsync();

            return Json(tipos);
        }

        [HttpGet]
        public async Task<IActionResult> GetRepuestosPorTaller(string search = null)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? int.Parse(User.FindFirstValue("TallerId") ?? "0");
            if (tallerId == 0)
            {
                return Json(new { success = false, message = "No se encontró taller asignado." });
            }

            var repuestos = await _context.taller_repuesto
                .Where(tr => tr.taller_id == tallerId && tr.activo)
                .Include(tr => tr.repuesto)
                .ThenInclude(r => r.repuesto_unidades)
                .Where(tr => string.IsNullOrEmpty(search) ||
                             tr.repuesto.sku.Contains(search) ||
                             tr.repuesto.nombre.Contains(search))
                .Select(tr => new
                {
                    id = tr.repuesto.repuesto_unidades.FirstOrDefault(ru => ru.taller_id == tallerId).id,
                    text = $"{tr.repuesto.sku} - {tr.repuesto.nombre} (Stock: {tr.repuesto.repuesto_unidades.FirstOrDefault(ru => ru.taller_id == tallerId).stock_disponible - (tr.repuesto.repuesto_unidades.FirstOrDefault(ru => ru.taller_id == tallerId).stock_reservado ?? 0)})",
                    stock = tr.repuesto.repuesto_unidades.FirstOrDefault(ru => ru.taller_id == tallerId).stock_disponible - (tr.repuesto.repuesto_unidades.FirstOrDefault(ru => ru.taller_id == tallerId).stock_reservado ?? 0),
                    precio = tr.repuesto.repuesto_unidades.FirstOrDefault(ru => ru.taller_id == tallerId).precio_unitario ?? 0
                })
                .ToListAsync();

            return Json(repuestos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarServicio(int atencion_id, int tipo_servicio_id, string descripcion, int precio_mano_obra, int sub_total, List<RepuestoData> repuestos)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Datos inválidos." });
            }

            var atencion = await _context.atencion.FindAsync(atencion_id);
            if (atencion == null)
            {
                return Json(new { success = false, message = "Atención no encontrada." });
            }

            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? int.Parse(User.FindFirstValue("TallerId") ?? "0");

            var nuevoServicio = new servicio
            {
                atencion_id = atencion_id,
                tipo_servicio_id = tipo_servicio_id,
                descripcion = descripcion,
                sub_total = sub_total,
                estado = "Pendiente"
            };

            _context.servicio.Add(nuevoServicio);
            await _context.SaveChangesAsync();

            foreach (var rep in repuestos ?? new List<RepuestoData>())
            {
                if (rep.cantidad > 0)
                {
                    var repUnidad = await _context.repuesto_unidades
                        .FirstOrDefaultAsync(ru => ru.id == rep.repuesto_unidades_id && ru.taller_id == tallerId);

                    if (repUnidad == null || (repUnidad.stock_disponible - (repUnidad.stock_reservado ?? 0)) < rep.cantidad)
                    {
                        return Json(new { success = false, message = $"Stock insuficiente para repuesto ID {rep.repuesto_unidades_id}." });
                    }

                    var servicioRepuesto = new servicio_repuesto
                    {
                        servicio_id = nuevoServicio.id,
                        repuesto_unidades_id = rep.repuesto_unidades_id,
                        cantidad = rep.cantidad
                    };

                    _context.servicio_repuesto.Add(servicioRepuesto);

                    repUnidad.stock_reservado = (repUnidad.stock_reservado ?? 0) + rep.cantidad;
                }
            }

            await _context.SaveChangesAsync();

            var servicioAgregado = await _context.servicio
                .Include(s => s.tipo_servicio)
                .Include(s => s.servicio_repuestos)
                    .ThenInclude(sr => sr.repuesto_unidades)
                        .ThenInclude(ru => ru.repuesto)
                .FirstOrDefaultAsync(s => s.id == nuevoServicio.id);

            return Json(new
            {
                success = true,
                servicio = new
                {
                    id = servicioAgregado.id,
                    tipo_nombre = servicioAgregado.tipo_servicio.nombre,
                    sub_total = servicioAgregado.sub_total,
                    descripcion = servicioAgregado.descripcion,
                    repuestos = servicioAgregado.servicio_repuestos.Select(sr => new
                    {
                        sku = sr.repuesto_unidades.repuesto.sku,
                        nombre = sr.repuesto_unidades.repuesto.nombre,
                        cantidad = sr.cantidad,
                        stock_disponible = sr.repuesto_unidades.stock_disponible - (sr.repuesto_unidades.stock_reservado ?? 0)
                    })
                }
            });
        }
    }

    public class RepuestoData
    {
        public int repuesto_unidades_id { get; set; }
        public int cantidad { get; set; }
    }
}
