using System.ComponentModel.DataAnnotations;
namespace Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Nombre de usuario obligatorio.")]
        [Display(Name = "Nombre de Usuario")]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
