namespace Pacientes.Api.Models;

public sealed class Paciente
{
    public int IdPaciente { get; set; }
    public required string Cedula { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public string? Direccion { get; set; }
}
