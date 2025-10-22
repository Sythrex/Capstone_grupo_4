using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class tipo_servicio
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public int? costo_base { get; set; }

    public virtual ICollection<servicio> servicios { get; set; } = new List<servicio>();
}
