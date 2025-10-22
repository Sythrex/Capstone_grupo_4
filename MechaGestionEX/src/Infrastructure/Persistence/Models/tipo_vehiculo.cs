using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class tipo_vehiculo
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public virtual ICollection<vehiculo> vehiculos { get; set; } = new List<vehiculo>();
}
