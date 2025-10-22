using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class categorium
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public virtual ICollection<repuesto> repuestos { get; set; } = new List<repuesto>();
}
