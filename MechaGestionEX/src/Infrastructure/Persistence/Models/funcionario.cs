using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class funcionario
{
    public int id { get; set; }

    public string rut { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? especialidad { get; set; }

    public bool activo { get; set; }

    public int? tipo_id { get; set; }

    public virtual ICollection<asignacion_tallere> asignacion_talleres { get; set; } = new List<asignacion_tallere>();

    public virtual ICollection<atencion> atencionadministrativos { get; set; } = new List<atencion>();

    public virtual ICollection<atencion> atencionmecanicos { get; set; } = new List<atencion>();

    public virtual ICollection<cotizacion> cotizacions { get; set; } = new List<cotizacion>();

    public virtual tipo_funcionario? tipo { get; set; }

    public virtual usuario? usuario { get; set; }
}
