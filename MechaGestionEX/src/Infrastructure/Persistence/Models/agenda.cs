using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Models
{
    public partial class agenda
    {
        public int id { get; set; }

        [Required]
        public DateTime fecha_creacion { get; set; } = DateTime.Now;

        [Required]
        public DateTime fecha_agenda { get; set; }

        [Required]
        [StringLength(50)]
        public string titulo { get; set; }

        [StringLength(20)]
        public string? estado { get; set; }

        [StringLength(200)]
        public string? comentarios { get; set; }

        public virtual atencion? atencion { get; set; }

    }
}
