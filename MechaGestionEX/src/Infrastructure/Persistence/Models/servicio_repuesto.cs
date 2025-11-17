using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class servicio_repuesto
{
    public int id { get; set; }

    public int servicio_id { get; set; }

    public int repuesto_unidades_id { get; set; }

    public int cantidad { get; set; } = 1;

    public virtual servicio servicio { get; set; } = null!;

    public virtual repuesto_unidades repuesto_unidades { get; set; } = null!;
}
