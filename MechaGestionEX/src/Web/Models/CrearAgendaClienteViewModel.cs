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

        [Required(ErrorMessage = "Debe asignar un vehículo para la atención.")]
        [Display(Name = "Vehículo")]
        public int? VehiculoId { get; set; }

        [Required(ErrorMessage = "Seleccione un taller")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un taller válido")]
        [Display(Name = "Taller")]
        public int TallerId { get; set; }

        public SelectList? TalleresDisponibles { get; set; }

        public SelectList? VehiculosDisponibles { get; set; }
    }
}