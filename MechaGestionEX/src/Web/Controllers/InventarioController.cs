using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Funcionario")]
    public class InventarioController : Controller
    {
        private readonly TallerMecanicoContext _context;

        public InventarioController(TallerMecanicoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search = null, int? categoriaId = null)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId");
            if (!tallerId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var query = from tr in _context.taller_repuesto.Where(tr => tr.taller_id == tallerId.Value && tr.activo)
                        join r in _context.repuesto on tr.repuesto_id equals r.id
                        join ru in _context.repuesto_unidades on new { RepuestoId = r.id, TallerId = tallerId.Value } equals new { RepuestoId = ru.repuesto_id, TallerId = ru.taller_id } into ruGroup
                        from ru in ruGroup.DefaultIfEmpty()
                        select new InventarioViewModel
                        {
                            RepuestoId = r.id,
                            Sku = r.sku,
                            Nombre = r.nombre,
                            Marca = r.marca,
                            CategoriaNombre = r.categoria != null ? r.categoria.nombre : "Sin Categoría",
                            StockDisponible = ru != null ? ru.stock_disponible : 0,
                            StockReservado = ru != null ? ru.stock_reservado ?? 0 : 0,
                            PrecioUnitario = ru != null ? ru.precio_unitario ?? 0 : 0
                        };

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => i.Nombre.Contains(search) || i.Sku.Contains(search));
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(i => i.CategoriaId == categoriaId); // Asume que agregas CategoriaId a ViewModel
            }

            var inventario = await query.ToListAsync();
            ViewBag.Categorias = await _context.categoria.ToListAsync(); // Para dropdown

            return View(inventario);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarRepuesto(int repuestoId, string nuevoNombre = null, string nuevoSku = null, string nuevaMarca = null, int? nuevaCategoriaId = null)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            repuesto repuesto;
            if (repuestoId == 0) // Crear nuevo
            {
                if (string.IsNullOrEmpty(nuevoNombre) || string.IsNullOrEmpty(nuevoSku))
                {
                    return Json(new { success = false, message = "Datos requeridos para nuevo repuesto." });
                }

                repuesto = new repuesto
                {
                    sku = nuevoSku,
                    nombre = nuevoNombre,
                    marca = nuevaMarca,
                    categoria_id = nuevaCategoriaId
                };
                _context.repuesto.Add(repuesto);
                await _context.SaveChangesAsync();
            }
            else
            {
                repuesto = await _context.repuesto.FindAsync(repuestoId);
                if (repuesto == null) return Json(new { success = false, message = "Repuesto no encontrado." });
            }

            // Verificar si ya asignado
            var asignacion = await _context.taller_repuesto
                .FirstOrDefaultAsync(tr => tr.taller_id == tallerId && tr.repuesto_id == repuesto.id);

            if (asignacion != null)
            {
                return Json(new { success = false, message = "Repuesto ya asignado." });
            }

            // Asignar
            var nuevaAsignacion = new taller_repuesto
            {
                taller_id = tallerId,
                repuesto_id = repuesto.id,
                activo = true,
                fecha_asignacion = DateOnly.FromDateTime(DateTime.Now)
            };
            _context.taller_repuesto.Add(nuevaAsignacion);

            // Crear unidades con stock 0
            var nuevasUnidades = new repuesto_unidades
            {
                repuesto_id = repuesto.id,
                taller_id = tallerId,
                stock_disponible = 0,
                stock_reservado = 0,
                precio_unitario = 0 // O un default
            };
            _context.repuesto_unidades.Add(nuevasUnidades);

            await _context.SaveChangesAsync();

            // Log inicial (opcional)
            var log = new log_inventario
            {
                repuesto_unidades_id = nuevasUnidades.id,
                usuario_id = usuarioId,
                variacion_stock = 0,
                nota = "Asignación inicial de repuesto",
                fecha_log = DateTime.Now
            };
            _context.log_inventario.Add(log);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarStock(int repuestoUnidadesId, int variacion, string nota, int? nuevoPrecio = null)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var unidades = await _context.repuesto_unidades
                .FirstOrDefaultAsync(ru => ru.id == repuestoUnidadesId && ru.taller_id == tallerId);

            if (unidades == null) return Json(new { success = false, message = "Stock no encontrado." });

            unidades.stock_disponible += variacion;
            if (unidades.stock_disponible < 0) return Json(new { success = false, message = "Stock no puede ser negativo." });

            if (nuevoPrecio.HasValue) unidades.precio_unitario = nuevoPrecio.Value;

            var log = new log_inventario
            {
                repuesto_unidades_id = unidades.id,
                usuario_id = usuarioId,
                variacion_stock = variacion,
                nota = nota,
                fecha_log = DateTime.Now
            };
            _context.log_inventario.Add(log);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetRepuestosGlobales(string search)
        {
            var repuestos = await _context.repuesto
                .Where(r => string.IsNullOrEmpty(search) || r.nombre.Contains(search) || r.sku.Contains(search))
                .Select(r => new { id = r.id, text = r.nombre + " (" + r.sku + ")" })
                .ToListAsync();

            return Json(repuestos);
        }

        // Acción para ver logs
        public async Task<IActionResult> VerLogs(int repuestoUnidadesId)
        {
            var logs = await _context.log_inventario
                .Where(l => l.repuesto_unidades_id == repuestoUnidadesId)
                .Include(l => l.usuario)
                .OrderByDescending(l => l.fecha_log)
                .ToListAsync();

            return PartialView("_LogsPartial", logs); // Crea esta partial view
        }
    }
}