namespace Web.Models
{
    public class AtencionHistRow
    {
        public int AtencionId { get; set; }
        public int? AgendaId { get; set; }
        public DateTime? FechaHora { get; set; }
        public string? Estado { get; set; }
        public string? Mecanico { get; set; }
        public string? Taller { get; set; }
        public string? Vehiculo { get; set; }
        public string? Observaciones { get; set; }
    }
}
