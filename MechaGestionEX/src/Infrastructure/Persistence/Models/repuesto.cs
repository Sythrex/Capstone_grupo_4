using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class repuesto
{
    public int id { get; set; }

    public string sku { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? marca { get; set; }

    public int? categoria_id { get; set; }

    public virtual categorium? categoria { get; set; }

    public virtual ICollection<repuesto_unidades> repuesto_unidades { get; set; } = new List<repuesto_unidades>();

    public virtual ICollection<taller_repuesto> taller_repuestos { get; set; } = new List<taller_repuesto>();
}
