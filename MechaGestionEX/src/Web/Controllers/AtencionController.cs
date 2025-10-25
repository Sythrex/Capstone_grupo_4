using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Web.Controllers
{
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
                .FirstOrDefaultAsync(a => a.agenda_id == id);

            // error 404 si no encuentra id atencion
            if (atencion == null) {
                return NotFound();
            }
            return View(atencion);
        }

        public async Task<IActionResult> Editar_Atencion(int id)
        {
            var atencion = await _context.atencion.FindAsync(id);
            if (atencion == null) {
                return NotFound();
            }
            return View(atencion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("id,kilometraje_ingreso,observaciones,estado,taller_id,vehiculo_id,mecanico_id,administrativo_id,cliente_id,cotizacion_id,agenda_id,fecha_ingreso")] atencion atencion)
        {
            if (id != atencion.id) {
                return NotFound();
            }
            if (ModelState.IsValid) {
                try {
                    _context.Update(atencion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) {
                    if (!_context.atencion.Any(e => e.id == atencion.id)) {
                        return NotFound();
                    }
                    else {
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
            var atencion = await _context.atencion.Include(a => a.agenda).FirstOrDefaultAsync(a => a.id == id);
            if (atencion == null)
            {
                return NotFound();
            }

            try
            {
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
    }
}