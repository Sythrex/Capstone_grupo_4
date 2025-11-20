using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Models;

public partial class taller_repuesto
{
    public int id { get; set; }

    public int taller_id { get; set; }

    public int repuesto_id { get; set; }

    public bool activo { get; set; } = true;

    public DateOnly? fecha_asignacion { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public virtual taller taller { get; set; } = null!;

    public virtual repuesto repuesto { get; set; } = null!;
}
