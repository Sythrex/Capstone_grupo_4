using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using Web.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;

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
            {
                return View(model);
            }

            // AUTENTICACIÓN TEMPORAL
            bool esValido = false;
            usuario user = null;
            if (model.NombreUsuario == "admin" && model.Password == "123")
            {
                // usuario de mentiras
                esValido = true;
                user = new usuario { nombre_usuario = "admin", id = 1 };
            }

            /*
            var user = await _context.usuarios
                                .FirstOrDefaultAsync(u => u.nombre_usuario == model.NombreUsuario);

            if (user != null)
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.password_hash, model.Password);
                if (verificationResult == PasswordVerificationResult.Success)
                {
                    esValido = true;
                }
            }
            */

            if (esValido)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.nombre_usuario),
                    new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
