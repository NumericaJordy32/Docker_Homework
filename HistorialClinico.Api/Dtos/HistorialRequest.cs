using System.ComponentModel.DataAnnotations;

namespace HistorialClinico.Api.Dtos;

public sealed class HistorialRequest
{
    [Range(1, int.MaxValue)] public int IdPaciente { get; set; }
    [Required, StringLength(30)] public string NumHistoria { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Diagnostico { get; set; } = string.Empty;
    [StringLength(500)] public string? Tratamiento { get; set; }
    public DateTime? Fecha { get; set; }
}
