using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Web.Models;

namespace web.Controllers
{
    public class AccountController : Controller
    {
        private readonly TallerMecanicoContext _context;

        public AccountController(TallerMecanicoContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.usuario
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.nombre_usuario == model.NombreUsuario);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
                return View(model);
            }

            var hash = ComputeSha256(model.Password);
            if (!string.Equals(user.password_hash, hash, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
                return View(model);
            }

            // Claims básicos
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.nombre_usuario ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString())
            };

            if (user.cliente_id.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Cliente"));
            }
            else if (user.funcionario_id.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Funcionario"));
                claims.Add(new Claim("FuncionarioId", user.funcionario_id.Value.ToString()));

                var hoy = DateOnly.FromDateTime(DateTime.Now);
                var asignacionActual = await _context.asignacion_talleres
                    .Where(a => a.funcionario_id == user.funcionario_id.Value &&
                                a.fecha_inicio <= hoy &&
                                (a.fecha_termino >= hoy || a.fecha_termino == null))
                    .OrderByDescending(a => a.ultimo_activo)
                    .ThenByDescending(a => a.fecha_inicio)
                    .FirstOrDefaultAsync();

                if (asignacionActual != null)
                {
                    claims.Add(new Claim("TallerId", asignacionActual.taller_id.ToString()));
                    HttpContext.Session.SetInt32("TallerId", asignacionActual.taller_id);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "No tienes un taller asignado activo.");
                    return View(model);
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true }
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.cliente_id.HasValue)
                return RedirectToAction("Panel", "Cliente");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> ChangeTaller(int tallerId)
        {
            var funcionarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var asignacion = await _context.asignacion_talleres
                .FirstOrDefaultAsync(a => a.funcionario_id == funcionarioId && a.taller_id == tallerId);

            if (asignacion == null)
            {
                return Json(new { success = false, message = "Taller no asignado." });
            }

            var asignacionesFuncionario = await _context.asignacion_talleres
                .Where(a => a.funcionario_id == funcionarioId)
                .ToListAsync();

            foreach (var a in asignacionesFuncionario)
            {
                a.ultimo_activo = (a.taller_id == tallerId);
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("TallerId", tallerId);

            return Json(new { success = true });
        }


        [HttpGet("Account/RegisterFuncionario")]
        public async Task<IActionResult> RegisterFuncionario()
        {
            ViewBag.TiposFuncionario = new SelectList(await _context.tipo_funcionario.ToListAsync(), "id", "nombre");
            ViewBag.Talleres = new SelectList(await _context.taller.ToListAsync(), "id", "razon_social");
            return View();
        }

        [HttpPost("Account/RegisterFuncionario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterFuncionario(RegisterFuncionarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TiposFuncionario = new SelectList(await _context.tipo_funcionario.ToListAsync(), "id", "nombre", vm.TipoId);
                ViewBag.Talleres = new SelectList(await _context.taller.ToListAsync(), "id", "razon_social", vm.TallerId);
                return View(vm);
            }

            // Validar usuario único
            var existeUsuario = await _context.usuario.AnyAsync(u => u.nombre_usuario == vm.NombreUsuario);
            if (existeUsuario)
            {
                ModelState.AddModelError(nameof(vm.NombreUsuario), "El nombre de usuario ya existe.");
                ViewBag.TiposFuncionario = new SelectList(await _context.tipo_funcionario.ToListAsync(), "id", "nombre", vm.TipoId);
                ViewBag.Talleres = new SelectList(await _context.taller.ToListAsync(), "id", "razon_social", vm.TallerId);
                return View(vm);
            }

            var nuevoFuncionario = new funcionario
            {
                rut = vm.Rut,
                nombre = vm.Nombre,
                especialidad = vm.Especialidad,
                activo = true,
                tipo_id = vm.TipoId
            };
            _context.funcionario.Add(nuevoFuncionario);
            await _context.SaveChangesAsync();

            var nuevaAsignacion = new asignacion_talleres
            {
                funcionario_id = nuevoFuncionario.id,
                taller_id = vm.TallerId,
                fecha_inicio = DateOnly.FromDateTime(DateTime.Now),
                created_at = DateOnly.FromDateTime(DateTime.Now)
            };
            _context.asignacion_talleres.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();

            var nuevoUsuario = new usuario
            {
                nombre_usuario = vm.NombreUsuario,
                password_hash = ComputeSha256(vm.Password),
                funcionario_id = nuevoFuncionario.id
            };
            _context.usuario.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            await SignInComoFuncionario(nuevoUsuario, vm.TallerId);

            return RedirectToAction("Index", "Home");
        }

        private async Task SignInComoFuncionario(usuario u, int tallerId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, u.nombre_usuario ?? ""),
                new Claim(ClaimTypes.NameIdentifier, u.id.ToString()),
                new Claim(ClaimTypes.Role, "Funcionario"),
                new Claim("TallerId", tallerId.ToString()),
                new Claim("FuncionarioId", u.funcionario_id.Value.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true }
            );
        }

        // Helper SHA-256 
        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        [HttpGet]
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> GetTalleresAsignados(string search = null)
        {
            var funcionarioId = int.Parse(User.FindFirstValue("FuncionarioId") ?? "0");

            var talleres = await _context.asignacion_talleres
                .Where(a => a.funcionario_id == funcionarioId)
                .Select(a => new { id = a.taller_id, text = a.taller.razon_social })
                .Where(t => string.IsNullOrEmpty(search) || t.text.Contains(search))
                .ToListAsync();

            return Json(talleres);
        }

        [HttpGet]
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> GetTallerActivo()
        {
            var funcionarioId = int.Parse(User.FindFirstValue("FuncionarioId") ?? "0");
            var tallerId = HttpContext.Session.GetInt32("TallerId") ?? int.Parse(User.FindFirstValue("TallerId") ?? "0");

            var isAssigned = await _context.asignacion_talleres
                .AnyAsync(a => a.funcionario_id == funcionarioId && a.taller_id == tallerId);

            if (!isAssigned)
            {
                return Json(new { id = 0, text = "Ninguno" });
            }

            var taller = await _context.taller.FirstOrDefaultAsync(t => t.id == tallerId);
            return Json(new { id = taller?.id ?? 0, text = taller?.razon_social ?? "Ninguno" });
        }

    }
}