using Infrastructure.Persistence;
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

            // error 404
            if (atencion == null)
            {
                return NotFound();
            }

            return View(atencion);
        }
    }
}