using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class usuario
{
    public int id { get; set; }

    public string? password_hash { get; set; }

    public string? nombre_usuario { get; set; }

    public int? cliente_id { get; set; }

    public int? funcionario_id { get; set; }

    public virtual cliente? cliente { get; set; }

    public virtual funcionario? funcionario { get; set; }

    public virtual ICollection<log_inventario> log_inventarios{ get; set; } = new List<log_inventario>();
}

