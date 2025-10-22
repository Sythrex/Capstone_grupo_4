using System;
using System.Collections.Generic;

namespace Infrastructure.Persistence.Models;

public partial class region
{
    public int id { get; set; }

    public string nombre { get; set; } = null!;

    public virtual ICollection<comuna> comunas { get; set; } = new List<comuna>();
}
