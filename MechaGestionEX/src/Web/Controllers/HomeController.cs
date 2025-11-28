using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;
[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    private string GetHomeUrl()
    {
        
        if (!(User?.Identity?.IsAuthenticated ?? false))
            return Url.Action("Login", "Account");
        if (User.IsInRole("Cliente"))
            return Url.Action("Panel", "Cliente");


        if (User.IsInRole("Funcionario"))
            return Url.Action("Index", "Home"); 


        return Url.Action("Index", "Home");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.UrlInicio = GetHomeUrl();

        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [AllowAnonymous]
    public IActionResult ErrorStatusCode(int code)
    {
        Response.StatusCode = code;
        ViewBag.StatusCode = code;
        ViewBag.UrlInicio = GetHomeUrl();

        return code switch
        {
            404 => View("~/Views/Shared/Error404.cshtml"),
            500 => View("~/Views/Shared/Error500.cshtml"),
            _ => View("~/Views/Shared/ErrorGeneric.cshtml"),
        };
    }

}
