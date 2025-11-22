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
using System.Text.RegularExpressions;
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.nombre_usuario ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString())
            };

            if (user.cliente_id.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Cliente"));
                claims.Add(new Claim("ClienteId", user.cliente_id.Value.ToString()));
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
            else
            {
                ModelState.AddModelError(string.Empty, "Usuario no autorizado.");
                return View(model);
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
                rut = LimpiarRut(vm.Rut),
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

        // ====== NUEVO ENDPOINT VALIDACIÓN RUT CLIENTE ======
        [HttpGet("Account/ValidarRutCliente")]
        public async Task<IActionResult> ValidarRutCliente(string rut)
        {
            if (string.IsNullOrWhiteSpace(rut))
                return Json(new { validFormato = false, dvCorrecto = false, disponible = false, mensaje = "RUT vacío." });

            rut = rut.Trim();
            var limpio = LimpiarRut(rut);
            var formateado = FormatearRut(limpio);

            bool formatoOk = Regex.IsMatch(formateado, @"^\d{1,2}\.\d{3}\.\d{3}-[\dkK]$");
            var partes = limpio.Split('-');
            bool dvOk = partes.Length == 2 && CalcularDv(partes[0]) == partes[1].ToUpper();
            bool existe = await _context.cliente.AnyAsync(c => c.rut == limpio);
            bool disponible = !existe;

            string mensaje = "";
            if (!formatoOk) mensaje = "Formato inválido.";
            else if (!dvOk) mensaje = "Dígito verificador incorrecto.";
            else if (!disponible) mensaje = "RUT ya registrado.";

            return Json(new
            {
                validFormato = formatoOk,
                dvCorrecto = dvOk,
                disponible,
                mensaje,
                rutFormateado = formateado
            });
        }

        // ========= REGISTRO CLIENTE =========
        [HttpGet]
        public async Task<IActionResult> RegisterClient()
        {
            // Cargar regiones para el select
            ViewBag.Regions = await _context.region
                .OrderBy(r => r.nombre)
                .Select(r => new { r.id, r.nombre })
                .ToListAsync();

            return View(new RegisterClientViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClient(RegisterClientViewModel vm, int? RegionId)
        {
            // Re-cargar regiones por si hay error
            ViewBag.Regions = await _context.region
                .OrderBy(r => r.nombre)
                .Select(r => new { r.id, r.nombre })
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            // Validar usuario único
            var existeUsuario = await _context.usuario.AnyAsync(u => u.nombre_usuario == vm.NombreUsuario);
            if (existeUsuario)
            {
                ModelState.AddModelError(nameof(vm.NombreUsuario), "El nombre de usuario ya existe.");
                return View(vm);
            }

            // Validar RUT único
            var rutLimpio = LimpiarRut(vm.Rut);
            var existeRut = await _context.cliente.AnyAsync(c => c.rut == rutLimpio);
            if (existeRut)
            {
                ModelState.AddModelError(nameof(vm.Rut), "El RUT ya está registrado.");
                return View(vm);
            }

            // Crear cliente
            var nuevoCliente = new cliente
            {
                rut = rutLimpio,
                nombre = vm.Nombre,
                correo = vm.Correo,
                telefono = vm.Telefono,
                direccion = vm.Direccion,
                comuna_id = vm.ComunaId
            };
            _context.cliente.Add(nuevoCliente);
            await _context.SaveChangesAsync();

            // Crear usuario
            var nuevoUsuario = new usuario
            {
                nombre_usuario = vm.NombreUsuario,
                password_hash = ComputeSha256(vm.Password),
                cliente_id = nuevoCliente.id
            };
            _context.usuario.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // Autologin
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nuevoUsuario.nombre_usuario ?? ""),
                new Claim(ClaimTypes.NameIdentifier, nuevoUsuario.id.ToString()),
                new Claim(ClaimTypes.Role, "Cliente"),
                new Claim("ClienteId", nuevoCliente.id.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Panel", "Cliente");
        }

        // Comunas por región usado por el JS del formulario
        [HttpGet]
        public async Task<IActionResult> ComunasPorRegion(int regionId)
        {
            var comunas = await _context.comuna
                .Where(c => c.region_id == regionId)
                .OrderBy(c => c.nombre)
                .Select(c => new { c.id, c.nombre })
                .ToListAsync();

            return Json(comunas);
        }

        // Helpers RUT
        private static string LimpiarRut(string rut)
        {
            rut = rut.ToUpper().Replace(".", "").Replace("-", "");
            if (rut.Length < 2) return rut;
            var cuerpo = rut[..^1];
            var dv = rut[^1].ToString();
            return cuerpo + "-" + dv;
        }

        private static string FormatearRut(string rutLimpioConGuion)
        {
            var partes = rutLimpioConGuion.Split('-');
            if (partes.Length != 2) return rutLimpioConGuion;
            var cuerpo = partes[0];
            var dv = partes[1];
            var rev = "";
            int contador = 0;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                rev = cuerpo[i] + rev;
                contador++;
                if (contador == 3 && i != 0)
                {
                    rev = "." + rev;
                    contador = 0;
                }
            }
            return rev + "-" + dv;
        }

        private static string CalcularDv(string cuerpo)
        {
            int suma = 0;
            int multiplicador = 2;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                suma += (cuerpo[i] - '0') * multiplicador;
                multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
            }
            int resto = 11 - (suma % 11);
            if (resto == 11) return "0";
            if (resto == 10) return "K";
            return resto.ToString();
        }
    }
}