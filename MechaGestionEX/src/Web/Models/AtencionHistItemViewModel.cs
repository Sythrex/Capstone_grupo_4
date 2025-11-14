namespace Web.Models
{
    public class AtencionHistItemViewModel
    {
        public int Id { get; set; }              // atencion.id (para mostrar)
        public int? AgendaId { get; set; }       // ¡para el link a Detalle!
        public string? EtiquetaSuperior { get; set; } // ej: patente
        public string? Subtitulo { get; set; }        // ej: observaciones
        public string? Estado { get; set; }           // atencion.estado
        public string? FechaTexto { get; set; }       // fecha formateada
    }
}