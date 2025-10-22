using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class cotizacion
{
    public int id { get; set; }

    public int cliente_id { get; set; }

    public int funcionario_cotizacion_id { get; set; }

    public DateOnly fecha_cotizacion { get; set; }

    public DateOnly fecha_vencimiento { get; set; }

    public string estado { get; set; } = null!;

    public int? monto_total { get; set; }

    public virtual ICollection<atencion> atencions { get; set; } = new List<atencion>();

    public virtual cliente cliente { get; set; } = null!;

    public virtual funcionario funcionario_cotizacion { get; set; } = null!;
}
