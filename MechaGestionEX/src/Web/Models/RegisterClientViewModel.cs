using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Web.Models
{
    public class RegisterClientViewModel
    {
        [Display(Name = "Nombre de usuario")]
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string NombreUsuario { get; set; } = "";

        [Display(Name = "Correo (opcional)")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [MaxLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        public string? Correo { get; set; }

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = "";

        [Display(Name = "Confirmar contraseña")]
        [Required(ErrorMessage = "Debes confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = "";

        [Display(Name = "RUT")]
        [Required(ErrorMessage = "El RUT es obligatorio.")]
        [MaxLength(13, ErrorMessage = "Máximo 13 caracteres.")]
        [RegularExpression(@"^\d{1,2}\.?\d{3}\.?\d{3}-[\dkK]$", ErrorMessage = "Formato de RUT inválido.")]
        public string Rut { get; set; } = "";

        [Display(Name = "Nombre y Apellido")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        public string Nombre { get; set; } = "";

        [Display(Name = "Teléfono (opcional)")]
        [MaxLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        public string? Telefono { get; set; }

        [Display(Name = "Dirección (opcional)")]
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string? Direccion { get; set; }

        [Required(ErrorMessage = "La comuna es obligatoria.")]
        public int? ComunaId { get; set; }

        public IEnumerable<SelectListItem>? Comunas { get; set; }
    }
}
