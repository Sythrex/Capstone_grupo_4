using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models
{
    public class MisDatosViewModel
    {
        public string Rut { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public int? ComunaId { get; set; }
        public SelectList? Comunas { get; set; }
    }
}