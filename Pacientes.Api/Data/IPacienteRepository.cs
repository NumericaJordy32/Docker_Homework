using Pacientes.Api.Dtos;
using Pacientes.Api.Models;

namespace Pacientes.Api.Data;

public interface IPacienteRepository
{
    Task<IReadOnlyList<Paciente>> GetAllAsync(CancellationToken ct);
    Task<Paciente?> GetByIdAsync(int id, CancellationToken ct);
    Task<Paciente> CreateAsync(PacienteRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, PacienteRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
