using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class asignacion_talleres
{
    public int id { get; set; }

    public int funcionario_id { get; set; }

    public int taller_id { get; set; }

    public DateOnly fecha_inicio { get; set; }

    public DateOnly fecha_termino { get; set; }

    public DateOnly? created_at { get; set; }

    public virtual funcionario funcionario { get; set; } = null!;

    public virtual taller taller { get; set; } = null!;
}
