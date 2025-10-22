using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class tipo_funcionario
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public virtual ICollection<funcionario> funcionarios { get; set; } = new List<funcionario>();
}
