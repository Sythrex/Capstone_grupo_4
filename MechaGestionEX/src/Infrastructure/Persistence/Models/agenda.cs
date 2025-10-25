using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class agenda
{
    public int id { get; set; }

    public DateTime fecha_agenda { get; set; }
    public DateTime creacion { get; set; }
    public required string estado { get; set; }
    public required string comentarios { get; set; }
    
}
