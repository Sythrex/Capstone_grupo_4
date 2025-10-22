using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class factura
{
    public int id { get; set; }

    public int folio { get; set; }

    public DateTime fecha_emision { get; set; }

    public int monto_neto { get; set; }

    public int iva { get; set; }

    public int? atencion_id { get; set; }

    public string? sii_params { get; set; }

    public int? sii_id { get; set; }

    public virtual atencion? atencion { get; set; }
}
