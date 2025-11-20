namespace Web.Models
{
    public class InventarioViewModel
    {
        public int RepuestoId { get; set; }
        public string Sku { get; set; }
        public string Nombre { get; set; }
        public string Marca { get; set; }
        public string CategoriaNombre { get; set; }
        public int CategoriaId { get; set; }
        public int StockDisponible { get; set; }
        public int StockReservado { get; set; }
        public int PrecioUnitario { get; set; }
    }
}