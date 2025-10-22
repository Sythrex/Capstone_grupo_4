using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class bitacora
{
    public int id { get; set; }

    public DateTime created_at { get; set; }

    public string? descripcion { get; set; }

    public int atencion_id { get; set; }

    public string? estado { get; set; }

    public string? tipo { get; set; }

    public virtual atencion atencion { get; set; } = null!;
}
