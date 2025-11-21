using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models
{
    public class VehiculoViewModel
    {
        public int Id { get; set; }
        public string Patente { get; set; } = null!;
        public string Vin { get; set; } = null!;
        public int Anio { get; set; }
        public int? Kilometraje { get; set; }
        public string? Color { get; set; }
        public int? TipoId { get; set; }
        public SelectList? TiposVehiculo { get; set; }
    }
}