using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class tipo_servicio
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public virtual ICollection<servicio> servicios { get; set; } = new List<servicio>();
}
