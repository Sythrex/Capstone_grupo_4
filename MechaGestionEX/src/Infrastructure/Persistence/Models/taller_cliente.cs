using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Models
{
    public class taller_cliente
    {
        public int id { get; set; }
        public int taller_id { get; set; }
        public int cliente_id { get; set; }
        public DateTime fecha_asociacion { get; set; } = DateTime.Now;

        public virtual taller taller { get; set; }
        public virtual cliente cliente { get; set; }
    }
}
