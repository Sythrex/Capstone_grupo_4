using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class repuesto
{
    public int id { get; set; }

    public string sku { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public bool usado { get; set; }

    public string? marca { get; set; }

    public int? categoria_id { get; set; }

    public virtual categorium? categoria { get; set; }

    public virtual ICollection<repuesto_unidade> repuesto_unidades { get; set; } = new List<repuesto_unidade>();
}
