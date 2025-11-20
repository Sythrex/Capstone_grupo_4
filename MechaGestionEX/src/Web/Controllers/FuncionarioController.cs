using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "Funcionario")]
    [Route("funcionarios")]
    public class FuncionariosController : Controller
    {
        private readonly TallerMecanicoContext _db;

        public FuncionariosController(TallerMecanicoContext db) => _db = db;

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? 
                int.Parse(User.FindFirstValue("TallerId") ?? "0");
            if (tallerId == 0) return BadRequest("No tienes taller asignado.");

            var data = await _db.funcionario
                .Include(f => f.tipo)
                .Where(f => _db.asignacion_talleres.Any(at => at.taller_id == tallerId && at.funcionario_id == f.id))
                .AsNoTracking()
                .OrderBy(f => f.id)
                .Take(100)
                .ToListAsync();

            return View(data);
        }

        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? 
                int.Parse(User.FindFirstValue("TallerId") ?? "0");
            var asociado = await _db.asignacion_talleres
                .AnyAsync(at => at.taller_id == tallerId && at.funcionario_id == id);
            if (!asociado) return Forbid();

            var entity = await _db.funcionario
                .Include(f => f.tipo)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Tipos = await _db.tipo_funcionario.AsNoTracking().ToListAsync();
            return View(new funcionario());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("rut,nombre,especialidad,activo,tipo_id")] funcionario model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tipos = await _db.tipo_funcionario.AsNoTracking().ToListAsync();
                return View(model);
            }

            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? 
                int.Parse(User.FindFirstValue("TallerId") ?? "0");
            if (tallerId == 0) return BadRequest("No tienes taller asignado.");

            var existe = await _db.funcionario.AnyAsync(f => f.rut == model.rut);
            if (existe)
            {
                ModelState.AddModelError("rut", "Ya existe un funcionario con este RUT.");
                ViewBag.Tipos = await _db.tipo_funcionario.AsNoTracking().ToListAsync();
                return View(model);
            }

            _db.funcionario.Add(model);
            await _db.SaveChangesAsync();

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            _db.asignacion_talleres.Add(new asignacion_talleres
            {
                funcionario_id = model.id,
                taller_id = tallerId,
                fecha_inicio = hoy,
                fecha_termino = hoy.AddYears(3),
                created_at = hoy
            });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? 
                int.Parse(User.FindFirstValue("TallerId") ?? "0");
            var asociado = await _db.asignacion_talleres
                .AnyAsync(at => at.taller_id == tallerId && at.funcionario_id == id);
            if (!asociado) return Forbid();

            var entity = await _db.funcionario
                .Include(f => f.tipo)
                .FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return NotFound();

            ViewBag.Tipos = await _db.tipo_funcionario.AsNoTracking().ToListAsync();
            return View(entity);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,rut,nombre,especialidad,activo,tipo_id")] funcionario model)
        {
            if (id != model.id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Tipos = await _db.tipo_funcionario.AsNoTracking().ToListAsync();
                return View(model);
            }

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? 
                int.Parse(User.FindFirstValue("TallerId") ?? "0");
            var asociado = await _db.asignacion_talleres
                .AnyAsync(at => at.taller_id == tallerId && at.funcionario_id == id);
            if (!asociado) return Forbid();

            var entity = await _db.funcionario.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        [HttpPost("delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _db.funcionario.FindAsync(id);
            if (entity != null)
            {
                var asignaciones = _db.asignacion_talleres.Where(at => at.funcionario_id == id);
                _db.asignacion_talleres.RemoveRange(asignaciones);

                _db.funcionario.Remove(entity);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}