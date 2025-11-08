using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "Funcionario")]
    [Route("clientes")]
    public class ClientesController : Controller
    {
        private readonly TallerMecanicoContext _db;

        public ClientesController(TallerMecanicoContext db) => _db = db;

        // GET: /clientes (Filtrado por taller_cliente)
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var tallerId = int.Parse(User.FindFirstValue("TallerId") ?? "0");
            if (tallerId == 0) return BadRequest("No tienes taller asignado.");

            var data = await _db.cliente
                .Include(c => c.comuna)
                    .ThenInclude(co => co.region)
                .Where(c => _db.taller_cliente.Any(tc => tc.taller_id == tallerId && tc.cliente_id == c.id))
                .AsNoTracking()
                .OrderBy(c => c.id)
                .Take(100)
                .ToListAsync();

            return View(data);
        }

        // GET: /clientes/details/5 (Con chequeo de asociación)
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var tallerId = int.Parse(User.FindFirstValue("TallerId") ?? "0");
            var asociado = await _db.taller_cliente.AnyAsync(tc => tc.taller_id == tallerId && tc.cliente_id == id);
            if (!asociado) return Forbid();

            var entity = await _db.cliente
                .Include(c => c.comuna).ThenInclude(co => co.region)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        // GET: /clientes/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Regions = await _db.region.AsNoTracking().ToListAsync();
            var comunasByRegion = await _db.comuna
                .AsNoTracking()
                .GroupBy(c => c.region_id)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());
            ViewBag.ComunasByRegion = comunasByRegion;
            return View(new cliente());
        }

        // POST: /clientes/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("rut,nombre,correo,telefono,direccion,comuna_id")] cliente model)
        {
            if (!ModelState.IsValid)
            {
                // Recarga ViewBags
                ViewBag.Regions = await _db.region.AsNoTracking().ToListAsync();
                var comunasByRegion = await _db.comuna.AsNoTracking().GroupBy(c => c.region_id).ToDictionaryAsync(g => g.Key, g => g.ToList());
                ViewBag.ComunasByRegion = comunasByRegion;
                return View(model);
            }

            var tallerId = int.Parse(User.FindFirstValue("TallerId") ?? "0");
            if (tallerId == 0) return BadRequest("No tienes taller asignado.");

            // Chequeo duplicado por RUT
            var existe = await _db.cliente.AnyAsync(c => c.rut == model.rut);
            if (existe)
            {
                ModelState.AddModelError("rut", "Ya existe un cliente con este RUT.");
                return View(model);
            }

            _db.cliente.Add(model);
            await _db.SaveChangesAsync();

            _db.taller_cliente.Add(new taller_cliente { taller_id = tallerId, cliente_id = model.id });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /clientes/edit/5
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var tallerId = int.Parse(User.FindFirstValue("TallerId") ?? "0");
            var asociado = await _db.taller_cliente.AnyAsync(tc => tc.taller_id == tallerId && tc.cliente_id == id);
            if (!asociado) return Forbid();

            var entity = await _db.cliente
                .Include(c => c.comuna).ThenInclude(co => co.region)
                .FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return NotFound();

            ViewBag.Regions = await _db.region.AsNoTracking().ToListAsync();
            var comunasByRegion = await _db.comuna.AsNoTracking().GroupBy(c => c.region_id).ToDictionaryAsync(g => g.Key, g => g.ToList());
            ViewBag.ComunasByRegion = comunasByRegion;

            return View(entity);
        }

        // POST: /clientes/edit/5
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,rut,nombre,correo,telefono,direccion,comuna_id")] cliente model)
        {
            if (id != model.id) return BadRequest();
            if (!ModelState.IsValid)
            {
                // Recarga ViewBags...
                return View(model);
            }

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /clientes/delete/5
        [HttpGet("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tallerId = int.Parse(User.FindFirstValue("TallerId") ?? "0");
            var asociado = await _db.taller_cliente.AnyAsync(tc => tc.taller_id == tallerId && tc.cliente_id == id);
            if (!asociado) return Forbid();

            var entity = await _db.cliente
                .Include(c => c.comuna).ThenInclude(co => co.region)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        // POST: /clientes/delete/5
        [HttpPost("delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _db.cliente.FindAsync(id);
            if (entity != null)
            {
                var asociaciones = _db.taller_cliente.Where(tc => tc.cliente_id == id);
                _db.taller_cliente.RemoveRange(asociaciones);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("check-rut")]
        public async Task<IActionResult> CheckRut(string rut)
        {
            var cliente = await _db.cliente
                .Include(c => c.comuna).ThenInclude(co => co.region)
                .FirstOrDefaultAsync(c => c.rut == rut);
            if (cliente == null) return Json(new { exists = false });

            return Json(new
            {
                exists = true,
                id = cliente.id,
                nombre = cliente.nombre,
                correo = cliente.correo,
                telefono = cliente.telefono,
                direccion = cliente.direccion,
                comuna = cliente.comuna?.nombre,
                region = cliente.comuna?.region?.nombre
            });
        }

        [HttpPost("add-to-taller")]
        public async Task<IActionResult> AddToTaller(int clienteId)
        {
            var tallerId = int.Parse(User.FindFirstValue("TallerId") ?? "0");
            if (tallerId == 0) return BadRequest("No tienes taller asignado.");

            var existeAsociacion = await _db.taller_cliente.AnyAsync(tc => tc.taller_id == tallerId && tc.cliente_id == clienteId);
            if (existeAsociacion) return Json(new { success = false, message = "Ya está asociado." });

            _db.taller_cliente.Add(new taller_cliente { taller_id = tallerId, cliente_id = clienteId });
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}