using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web.ViewModels
{
    public class CrearAgendaViewModel
    {
        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(50)]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una fecha y hora.")]
        [Display(Name = "Fecha y Hora de la Cita")]
        public DateTime FechaAgenda { get; set; }

        [Display(Name = "Observaciones Iniciales")]
        [StringLength(200)]
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un cliente.")]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Display(Name = "Vehículo (Opcional)")]
        public int? VehiculoId { get; set; }

        public IEnumerable<SelectListItem>? ClientesDisponibles { get; set; }
    }
}