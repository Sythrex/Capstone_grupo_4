using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class log_inventario
{
    public long id { get; set; }

    public DateTime fecha_log { get; set; }

    public int repuesto_unidades_id { get; set; }

    public int variacion_stock { get; set; }

    public string? nota { get; set; }

    public virtual repuesto_unidade repuesto_unidades { get; set; } = null!;
}
