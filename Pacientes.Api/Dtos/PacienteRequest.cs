using System.ComponentModel.DataAnnotations;

namespace Pacientes.Api.Dtos;

public sealed class PacienteRequest
{
    [Required, StringLength(20)] public string Cedula { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Apellido { get; set; } = string.Empty;
    [StringLength(250)] public string? Direccion { get; set; }
}
