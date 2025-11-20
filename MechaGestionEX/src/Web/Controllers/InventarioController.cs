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
                query = query.Where(i => i.CategoriaId == categoriaId);
            }

            var inventario = await query.ToListAsync();
            ViewBag.Categorias = await _context.categoria.ToListAsync();

            return View(inventario);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarRepuesto(int repuestoId, string nuevoNombre = null, string nuevoSku = null, string nuevaMarca = null, int? nuevaCategoriaId = null)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            repuesto repuesto;
            if (repuestoId == 0)
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

            var asignacion = await _context.taller_repuesto
                .FirstOrDefaultAsync(tr => tr.taller_id == tallerId && tr.repuesto_id == repuesto.id);

            if (asignacion != null)
            {
                return Json(new { success = false, message = "Repuesto ya asignado." });
            }

            var nuevaAsignacion = new taller_repuesto
            {
                taller_id = tallerId,
                repuesto_id = repuesto.id,
                activo = true,
                fecha_asignacion = DateOnly.FromDateTime(DateTime.Now)
            };
            _context.taller_repuesto.Add(nuevaAsignacion);

            var nuevasUnidades = new repuesto_unidades
            {
                repuesto_id = repuesto.id,
                taller_id = tallerId,
                stock_disponible = 0,
                stock_reservado = 0,
                precio_unitario = 0 // default
            };
            _context.repuesto_unidades.Add(nuevasUnidades);

            await _context.SaveChangesAsync();

            var log = new log_inventario
            {
                repuesto_unidades_id = nuevasUnidades.id,
                usuario_id = usuarioId,
                variacion_stock = 0,
                stock_anterior = 0,
                stock_nuevo = 0,
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

            var stockAnterior = unidades.stock_disponible;

            unidades.stock_disponible += variacion;
            if (unidades.stock_disponible < 0) return Json(new { success = false, message = "Stock no puede ser negativo." });

            var stockNuevo = unidades.stock_disponible;

            if (nuevoPrecio.HasValue)
            {
                unidades.precio_unitario = nuevoPrecio.Value;
            }

            var log = new log_inventario
            {
                repuesto_unidades_id = unidades.id,
                usuario_id = usuarioId,
                variacion_stock = variacion,
                stock_anterior = stockAnterior,
                stock_nuevo = stockNuevo,
                nota = nota ?? "Actualización de stock",
                fecha_log = DateTime.Now
            };
            _context.log_inventario.Add(log);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetRepuestosGlobales(string search)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;

            var repuestos = await _context.repuesto
                .Where(r => string.IsNullOrEmpty(search) || r.nombre.Contains(search) || r.sku.Contains(search))
                .Select(r => new
                {
                    id = r.id,
                    sku = r.sku,
                    nombre = r.nombre,
                    marca = r.marca,
                    categoriaNombre = r.categoria != null ? r.categoria.nombre : "Sin Categoría",
                    asignado = _context.taller_repuesto.Any(tr => tr.taller_id == tallerId && tr.repuesto_id == r.id)
                })
                .ToListAsync();

            return Json(repuestos);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarRepuestosBatch(List<int> repuestosIds)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var messages = new List<string>();
            bool allSuccess = true;

            foreach (var repuestoId in repuestosIds)
            {
                var repuesto = await _context.repuesto.FindAsync(repuestoId);
                if (repuesto == null)
                {
                    messages.Add($"Repuesto ID {repuestoId} no encontrado.");
                    allSuccess = false;
                    continue;
                }

                var asignacion = await _context.taller_repuesto
                    .FirstOrDefaultAsync(tr => tr.taller_id == tallerId && tr.repuesto_id == repuesto.id);

                if (asignacion != null)
                {
                    messages.Add($"Repuesto ID {repuestoId} ya asignado.");
                    allSuccess = false;
                    continue;
                }

                var nuevaAsignacion = new taller_repuesto
                {
                    taller_id = tallerId,
                    repuesto_id = repuesto.id,
                    activo = true,
                    fecha_asignacion = DateOnly.FromDateTime(DateTime.Now)
                };
                _context.taller_repuesto.Add(nuevaAsignacion);

                var nuevasUnidades = new repuesto_unidades
                {
                    repuesto_id = repuesto.id,
                    taller_id = tallerId,
                    stock_disponible = 0,
                    stock_reservado = 0,
                    precio_unitario = 0
                };
                _context.repuesto_unidades.Add(nuevasUnidades);

                await _context.SaveChangesAsync();

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
            }

            if (allSuccess)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = string.Join("\n", messages) });
            }
        }

        public async Task<IActionResult> VerLogs(int id)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var unidades = await _context.repuesto_unidades
                .FirstOrDefaultAsync(ru => ru.id == id && ru.taller_id == tallerId);

            if (unidades == null)
            {
                return NotFound();
            }

            var logs = await _context.log_inventario
                .Where(l => l.repuesto_unidades_id == id)
                .Include(l => l.usuario)
                .OrderByDescending(l => l.fecha_log)
                .ToListAsync();

            return PartialView("_LogsPartial", logs);
        }

        [HttpGet]
        public async Task<IActionResult> GetInventarioData(int? categoriaId = null)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;

            var query = from ru in _context.repuesto_unidades.Where(ru => ru.taller_id == tallerId)
                        join r in _context.repuesto on ru.repuesto_id equals r.id
                        join tr in _context.taller_repuesto on new { ru.taller_id, ru.repuesto_id } equals new { taller_id = tr.taller_id, repuesto_id = tr.repuesto_id } into trGroup
                        from tr in trGroup.DefaultIfEmpty()
                        where tr != null && tr.activo
                        select new
                        {
                            RepuestoId = r.id,
                            RepuestoUnidadesId = ru.id,
                            Sku = r.sku,
                            Nombre = r.nombre,
                            Marca = r.marca,
                            CategoriaNombre = r.categoria != null ? r.categoria.nombre : "Sin Categoría",
                            CategoriaId = r.categoria_id ?? 0,
                            StockDisponible = ru.stock_disponible,
                            StockReservado = ru.stock_reservado ?? 0,
                            PrecioUnitario = ru.precio_unitario ?? 0
                        };

            if (categoriaId.HasValue)
            {
                query = query.Where(i => i.CategoriaId == categoriaId);
            }

            var inventario = await query.ToListAsync();
            return Json(inventario);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarStocksBatch(List<StockChange> cambios, string nota)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            bool allSuccess = true;
            var messages = new List<string>();

            foreach (var cambio in cambios)
            {
                var unidades = await _context.repuesto_unidades
                    .FirstOrDefaultAsync(ru => ru.id == cambio.repuestoUnidadesId && ru.taller_id == tallerId);

                if (unidades == null)
                {
                    messages.Add($"Stock ID {cambio.repuestoUnidadesId} no encontrado.");
                    allSuccess = false;
                    continue;
                }

                bool hasStockChange = false;

                if (cambio.variacionDisp != 0)
                {
                    var stockAnteriorDisp = unidades.stock_disponible;
                    unidades.stock_disponible += cambio.variacionDisp;
                    if (unidades.stock_disponible < 0)
                    {
                        messages.Add($"Stock disponible no puede ser negativo para ID {cambio.repuestoUnidadesId}.");
                        allSuccess = false;
                        continue;
                    }
                    var stockNuevoDisp = unidades.stock_disponible;
                    var logDisp = new log_inventario
                    {
                        repuesto_unidades_id = unidades.id,
                        usuario_id = usuarioId,
                        variacion_stock = cambio.variacionDisp,
                        stock_anterior = stockAnteriorDisp,
                        stock_nuevo = stockNuevoDisp,
                        nota = nota ?? "Actualización batch de stock disponible",
                        fecha_log = DateTime.Now
                    };
                    _context.log_inventario.Add(logDisp);
                    hasStockChange = true;
                }

                if (cambio.variacionRes != 0)
                {
                    var stockAnteriorRes = unidades.stock_reservado ?? 0;
                    unidades.stock_reservado = (unidades.stock_reservado ?? 0) + cambio.variacionRes;
                    if ((unidades.stock_reservado ?? 0) < 0)
                    {
                        messages.Add($"Stock reservado no puede ser negativo para ID {cambio.repuestoUnidadesId}.");
                        allSuccess = false;
                        continue;
                    }
                    var stockNuevoRes = unidades.stock_reservado ?? 0;
                    var logRes = new log_inventario
                    {
                        repuesto_unidades_id = unidades.id,
                        usuario_id = usuarioId,
                        variacion_stock = cambio.variacionRes,
                        stock_anterior = stockAnteriorRes,
                        stock_nuevo = stockNuevoRes,
                        nota = nota ?? "Actualización batch de stock reservado",
                        fecha_log = DateTime.Now
                    };
                    _context.log_inventario.Add(logRes);
                    hasStockChange = true;
                }

                if (cambio.nuevoPrecio.HasValue)
                {
                    unidades.precio_unitario = cambio.nuevoPrecio.Value;
                }

                if (!hasStockChange && !cambio.nuevoPrecio.HasValue)
                {
                    continue;
                }
            }

            if (allSuccess)
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = string.Join("\n", messages) });
            }
        }
        public class StockChange
        {
            public int repuestoUnidadesId { get; set; }
            public int variacionDisp { get; set; }
            public int variacionRes { get; set; }
            public int? nuevoPrecio { get; set; }
        }

        public async Task<IActionResult> Detalles(int id) // id = repuestoUnidadesId
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var unidades = await _context.repuesto_unidades
                .Include(ru => ru.repuesto)
                .ThenInclude(r => r.categoria)
                .FirstOrDefaultAsync(ru => ru.id == id && ru.taller_id == tallerId);

            if (unidades == null) return NotFound();

            var model = new InventarioDetalleViewModel
            {
                RepuestoUnidadesId = unidades.id,
                Sku = unidades.repuesto.sku,
                Nombre = unidades.repuesto.nombre,
                Marca = unidades.repuesto.marca,
                CategoriaNombre = unidades.repuesto.categoria?.nombre ?? "Sin Categoría",
                StockDisponible = unidades.stock_disponible,
                StockReservado = unidades.stock_reservado ?? 0,
                PrecioUnitario = unidades.precio_unitario ?? 0,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarStockUnitario(int repuestoUnidadesId, int variacionDisp, int variacionRes, int? nuevoPrecio, string nota)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var unidades = await _context.repuesto_unidades
                .FirstOrDefaultAsync(ru => ru.id == repuestoUnidadesId && ru.taller_id == tallerId);

            if (unidades == null) return Json(new { success = false, message = "Stock no encontrado." });

            bool hasStockChange = false;

            if (variacionDisp != 0)
            {
                var stockAnteriorDisp = unidades.stock_disponible;
                unidades.stock_disponible += variacionDisp;
                if (unidades.stock_disponible < 0) return Json(new { success = false, message = "Stock disponible no puede ser negativo." });
                var stockNuevoDisp = unidades.stock_disponible;
                var log = new log_inventario
                {
                    repuesto_unidades_id = unidades.id,
                    usuario_id = usuarioId,
                    variacion_stock = variacionDisp,
                    stock_anterior = stockAnteriorDisp,
                    stock_nuevo = stockNuevoDisp,
                    nota = nota ?? "Actualización unitaria de stock disponible",
                    fecha_log = DateTime.Now
                };
                _context.log_inventario.Add(log);
                hasStockChange = true;
            }

            if (variacionRes != 0)
            {
                var stockAnteriorRes = unidades.stock_reservado ?? 0;
                unidades.stock_reservado = (unidades.stock_reservado ?? 0) + variacionRes;
                if ((unidades.stock_reservado ?? 0) < 0) return Json(new { success = false, message = "Stock reservado no puede ser negativo." });
                var stockNuevoRes = unidades.stock_reservado ?? 0;
                var log = new log_inventario
                {
                    repuesto_unidades_id = unidades.id,
                    usuario_id = usuarioId,
                    variacion_stock = variacionRes,
                    stock_anterior = stockAnteriorRes,
                    stock_nuevo = stockNuevoRes,
                    nota = nota ?? "Actualización unitaria de stock reservado",
                    fecha_log = DateTime.Now
                };
                _context.log_inventario.Add(log);
                hasStockChange = true;
            }

            if (nuevoPrecio.HasValue)
            {
                unidades.precio_unitario = nuevoPrecio.Value;
            }

            if (hasStockChange || nuevoPrecio.HasValue)
            {
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarAsignacion(int repuestoUnidadesId)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId").Value;
            var unidades = await _context.repuesto_unidades.FirstOrDefaultAsync(ru => ru.id == repuestoUnidadesId && ru.taller_id == tallerId);
            if (unidades == null) return Json(new { success = false, message = "No encontrado." });

            var asignacion = await _context.taller_repuesto.FirstOrDefaultAsync(tr => tr.taller_id == tallerId && tr.repuesto_id == unidades.repuesto_id);
            if (asignacion != null)
            {
                asignacion.activo = false;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }
}