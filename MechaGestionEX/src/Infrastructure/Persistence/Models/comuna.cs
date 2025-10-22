using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class comuna
{
    public int id { get; set; }

    public int region_id { get; set; }

    public string nombre { get; set; } = null!;

    public virtual ICollection<cliente> clientes { get; set; } = new List<cliente>();

    public virtual region region { get; set; } = null!;

    public virtual ICollection<taller> tallers { get; set; } = new List<taller>();
}
