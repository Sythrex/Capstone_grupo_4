using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class repuesto_unidade
{
    public int id { get; set; }

    public int repuesto_id { get; set; }

    public int taller_id { get; set; }

    public int stock_disponible { get; set; }

    public int? stock_reservado { get; set; }

    public int? precio_unitario { get; set; }

    public virtual ICollection<log_inventario> log_inventarios { get; set; } = new List<log_inventario>();

    public virtual repuesto repuesto { get; set; } = null!;

    public virtual ICollection<servicio> servicios { get; set; } = new List<servicio>();

    public virtual taller taller { get; set; } = null!;
}
