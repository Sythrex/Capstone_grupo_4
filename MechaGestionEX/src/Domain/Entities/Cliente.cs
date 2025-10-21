namespace Domain.Entities;

public class Cliente
{
    public int Id { get; set; }

    // RUT chileno con guion (ej: 12.345.678-9 o 12345678-9)
    public string Rut { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
}