using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class taller
{
    public int id { get; set; }

    public string razon_social { get; set; } = null!;

    public string rut_taller { get; set; } = null!;

    public string direccion { get; set; } = null!;

    public int comuna_id { get; set; }

    public virtual ICollection<asignacion_tallere> asignacion_talleres { get; set; } = new List<asignacion_tallere>();

    public virtual ICollection<atencion> atencions { get; set; } = new List<atencion>();

    public virtual comuna comuna { get; set; } = null!;

    public virtual ICollection<repuesto_unidade> repuesto_unidades { get; set; } = new List<repuesto_unidade>();
}
