using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class cliente_vehiculo
{
    public int id { get; set; }

    public int cliente_id { get; set; }

    public int vehiculo_id { get; set; }

    public bool? principal { get; set; }

    public DateOnly fecha_desde { get; set; }

    public DateOnly? fecha_hasta { get; set; }

    public DateOnly created_at { get; set; }

    public virtual cliente cliente { get; set; } = null!;

    public virtual vehiculo vehiculo { get; set; } = null!;
}
