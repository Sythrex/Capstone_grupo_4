namespace Web.Models
{
    public class AtencionHistItemViewModel
    {
        public int Id { get; set; }
        public int? AgendaId { get; set; }
        public string? EtiquetaSuperior { get; set; }
        public string? Subtitulo { get; set; }
        public string? Estado { get; set; }
        public string? FechaTexto { get; set; }
    }
}