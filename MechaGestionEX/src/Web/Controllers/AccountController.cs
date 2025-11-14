using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Web.Models;
using System.Linq;

namespace web.Controllers
{
    public class AccountController : Controller
    {
        private readonly TallerMecanicoContext _context;
        //private readonly IPasswordHasher<usuario> _passwordHasher;

        //public AccountController(TallerMecanicoContext context, IPasswordHasher<usuario> passwordHasher)
        //{
        //    _context = context;
        //    _passwordHasher = passwordHasher;
        //}
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

            // 1) Buscar usuario por nombre_usuario (por ahora NO usamos email)
            var user = await _context.usuario
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.nombre_usuario == model.NombreUsuario);

            // 2) Validar existencia y hash SHA-256
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

            // 3) Claims básicos + rol inferido (según tus tablas)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.nombre_usuario ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString())
            };

            if (user.cliente_id.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Cliente"));
                claims.Add(new Claim("ClienteId", user.cliente_id.Value.ToString()));   // <-- clave
            }

            if (user.funcionario_id.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Funcionario"));
                claims.Add(new Claim("FuncionarioId", user.funcionario_id.Value.ToString()));
            }


            if (user.cliente_id.HasValue) claims.Add(new Claim(ClaimTypes.Role, "Cliente"));
            if (user.funcionario_id.HasValue) claims.Add(new Claim(ClaimTypes.Role, "Funcionario"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true }
            );

            // 4) Redirección
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.cliente_id.HasValue)
                return RedirectToAction("Panel", "Cliente");   // <- aquí el cambio

            return RedirectToAction("Index", "Home");

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet("Account/RegisterClient")]
        public async Task<IActionResult> RegisterClient()
        {
            // Cargamos regiones para la vista (como en tu ejemplo con ViewBag.Regions)
            ViewBag.Regions = await _context.region
                .AsNoTracking()
                .OrderBy(r => r.nombre)
                .Select(r => new { r.id, r.nombre })
                .ToListAsync();

            // Si usas también el ViewModel para selects:
            var vm = new RegisterClientViewModel
            {
                Comunas = Enumerable.Empty<SelectListItem>() // se cargan por AJAX
            };

            return View(vm);
        }

        [HttpPost("Account/RegisterClient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClient(RegisterClientViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Regions = await _context.region
                .AsNoTracking()
                .OrderBy(r => r.nombre)
                .Select(r => new { r.id, r.nombre })
                .ToListAsync();

                vm.Comunas = Enumerable.Empty<SelectListItem>();
                return View(vm);
            }

            // 1) Validar usuario único
            var existeUsuario = await _context.usuario
                .AnyAsync(u => u.nombre_usuario == vm.NombreUsuario);
            if (existeUsuario)
            {
                ModelState.AddModelError(nameof(vm.NombreUsuario), "El nombre de usuario ya existe.");
                ViewBag.Regions = await _context.region
                .AsNoTracking()
                .OrderBy(r => r.nombre)
                .Select(r => new { r.id, r.nombre })
                .ToListAsync();

                vm.Comunas = Enumerable.Empty<SelectListItem>();
                return View(vm);
            }

            // 2) (Sugerido) Validar RUT único
            var existeRut = await _context.cliente.AnyAsync(c => c.rut == vm.Rut);
            if (existeRut)
            {
                ModelState.AddModelError(nameof(vm.Rut), "Ya existe un cliente con ese RUT.");
                vm.Comunas = Enumerable.Empty<SelectListItem>();
                ViewBag.Regions = await _context.region
                .AsNoTracking()
                .OrderBy(r => r.nombre)
                .Select(r => new { r.id, r.nombre })
                .ToListAsync();

                vm.Comunas = Enumerable.Empty<SelectListItem>();
                return View(vm);
            }

            // 3) Crear cliente (en tu esquema el correo vive aquí)
            var nuevoCliente = new cliente
            {
                rut = vm.Rut,
                nombre = vm.Nombre,
                correo = vm.Correo,
                telefono = vm.Telefono,
                direccion = vm.Direccion,
                comuna_id = vm.ComunaId
            };
            _context.cliente.Add(nuevoCliente);
            await _context.SaveChangesAsync(); // obtiene id

            // 4) Crear usuario vinculado al cliente (SHA-256)
            var nuevoUsuario = new usuario
            {
                nombre_usuario = vm.NombreUsuario,
                password_hash = ComputeSha256(vm.Password),
                cliente_id = nuevoCliente.id
                // funcionario_id queda null
            };
            _context.usuario.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // 5) Login automático como Cliente
            await SignInComoCliente(nuevoUsuario);


            // 6) Redirige (ajusta cuando tengas Cliente/Panel)
            return RedirectToAction("Panel", "Cliente");
        }

        // ——— Helpers ———
        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private async Task SignInComoCliente(usuario u)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, u.nombre_usuario ?? ""),
                new Claim(ClaimTypes.NameIdentifier, u.id.ToString()),
                new Claim(ClaimTypes.Role, "Cliente"),
                new Claim("TipoUsuario", "Cliente"),
                new Claim("ClienteId", u.cliente_id?.ToString() ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true }
            );
        }

        [HttpGet("Account/ComunasPorRegion/{regionId:int}")]
        public async Task<IActionResult> ComunasPorRegion(int regionId)
        {
            var comunas = await _context.comuna
                .AsNoTracking()
                .Where(c => c.region_id == regionId)
                .OrderBy(c => c.nombre)
                .Select(c => new { id = c.id, nombre = c.nombre })
                .ToListAsync();

            return Json(comunas);
        }


    }
}
