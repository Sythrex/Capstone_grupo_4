using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("health")]
public class HealthController : Controller
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) => _db = db;

    [HttpGet("db")]
    public async Task<IActionResult> Db()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            return Ok(new { canConnect, provider = _db.Database.ProviderName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
