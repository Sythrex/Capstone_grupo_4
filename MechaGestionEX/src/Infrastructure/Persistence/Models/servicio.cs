using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class servicio
{
    public int id { get; set; }

    public int atencion_id { get; set; }

    public int tipo_servicio_id { get; set; }

    public int? sub_total { get; set; }

    public string? descripcion { get; set; }
    
    public string? estado { get; set; }

    public virtual atencion atencion { get; set; } = null!;

    public virtual tipo_servicio tipo_servicio { get; set; } = null!;

    public virtual ICollection<servicio_repuesto> servicio_repuestos { get; set; } = new List<servicio_repuesto>();
}
