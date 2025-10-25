using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class atencion
{
    public int id { get; set; }

    public DateTime fecha_ingreso { get; set; }

    public int kilometraje_ingreso { get; set; }

    public string? observaciones { get; set; }

    public string estado { get; set; } = null!;

    public int taller_id { get; set; }

    public int? vehiculo_id { get; set; }

    public int? mecanico_id { get; set; }

    public int administrativo_id { get; set; }

    public int? cliente_id { get; set; }

    public int? cotizacion_id { get; set; }

    public virtual funcionario administrativo { get; set; } = null!;

    public virtual ICollection<bitacora> bitacoras { get; set; } = new List<bitacora>();

    public virtual cliente? cliente { get; set; }

    public virtual cotizacion? cotizacion { get; set; }

    public virtual ICollection<factura> facturas { get; set; } = new List<factura>();

    public virtual funcionario? mecanico { get; set; }

    public virtual ICollection<servicio> servicios { get; set; } = new List<servicio>();

    public virtual taller taller { get; set; } = null!;

    public virtual vehiculo? vehiculo { get; set; }

    public int agenda_id { get; set; }
}
