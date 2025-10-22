using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class vehiculo
{
    public int id { get; set; }

    public string patente { get; set; } = null!;

    public string vin { get; set; } = null!;

    public int anio { get; set; }

    public int? kilometraje { get; set; }

    public string? color { get; set; }

    public int? tipo_id { get; set; }

    public virtual ICollection<atencion> atencions { get; set; } = new List<atencion>();

    public virtual ICollection<cliente_vehiculo> cliente_vehiculos { get; set; } = new List<cliente_vehiculo>();

    public virtual tipo_vehiculo? tipo { get; set; }
}
