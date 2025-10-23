using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Route("clientes")]
public class ClientesController : Controller
{
    private readonly TallerMecanicoContext _db;
    public ClientesController(TallerMecanicoContext db) => _db = db;

    // GET: /clientes
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var data = await _db.clientes
            .AsNoTracking()
            .OrderBy(c => c.id)
            .Take(100)
            .ToListAsync();

        return View(data);
    }

    // GET: /clientes/details/5
    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var entity = await _db.clientes.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
        if (entity is null) return NotFound();
        return View(entity);
    }

    // GET: /clientes/create
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new cliente());
    }

    // POST: /clientes/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("rut,nombre,correo,telefono,direccion,comuna_id")] cliente model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.clientes.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /clientes/edit/5
    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _db.clientes.FindAsync(id);
        if (entity is null) return NotFound();
        return View(entity);
    }

    // POST: /clientes/edit/5
    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("id,rut,nombre,correo,telefono,direccion,comuna_id")] cliente model)
    {
        if (id != model.id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /clientes/delete/5
    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.clientes.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
        if (entity is null) return NotFound();
        return View(entity);
    }

    // POST: /clientes/delete/5
    [HttpPost("delete/{id:int}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var entity = await _db.clientes.FindAsync(id);
        if (entity is not null)
        {
            _db.clientes.Remove(entity);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
