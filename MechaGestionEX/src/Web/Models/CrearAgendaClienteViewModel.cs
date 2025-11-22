using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class CrearAgendaClienteViewModel
    {
        [Required(ErrorMessage = "La fecha y hora son requeridas.")]
        [Display(Name = "Fecha y Hora")]
        public DateTime FechaAgenda { get; set; }


        [Required(ErrorMessage = "Las observaciones son requeridas.")]
        [Display(Name = "Observaciones")]
        public string Observaciones { get; set; } = string.Empty;

        [Display(Name = "Vehículo (opcional)")]
        public int? VehiculoId { get; set; }

        public SelectList? VehiculosDisponibles { get; set; }
    }
}