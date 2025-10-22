using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class usuario
{
    public int id { get; set; }

    public string password_hash { get; set; } = null!;

    public int? cliente_id { get; set; }

    public int? funcionario_id { get; set; }

    public virtual cliente? cliente { get; set; }

    public virtual funcionario? funcionario { get; set; }
}
