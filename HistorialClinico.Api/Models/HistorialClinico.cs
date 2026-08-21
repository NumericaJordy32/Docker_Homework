namespace HistorialClinico.Api.Models;

public sealed class HistorialClinico
{
    public int IdHistorialClinico { get; set; }
    public int IdPaciente { get; set; }
    public required string NumHistoria { get; set; }
    public required string Diagnostico { get; set; }
    public string? Tratamiento { get; set; }
    public DateTime Fecha { get; set; }
}
