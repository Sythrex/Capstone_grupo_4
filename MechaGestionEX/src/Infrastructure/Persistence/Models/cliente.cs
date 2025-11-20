using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class cliente
{
    public int id { get; set; }

    public string rut { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? correo { get; set; }

    public string? telefono { get; set; }

    public string? direccion { get; set; }

    public int? comuna_id { get; set; }

    public virtual ICollection<atencion> atencions { get; set; } = new List<atencion>();

    public virtual ICollection<cliente_vehiculo> cliente_vehiculos { get; set; } = new List<cliente_vehiculo>();

    public virtual ICollection<taller_cliente> taller_clientes { get; set; } = new List<taller_cliente>();

    public virtual comuna? comuna { get; set; }

    public virtual ICollection<cotizacion> cotizacions { get; set; } = new List<cotizacion>();

    public virtual usuario? usuario { get; set; }
}
