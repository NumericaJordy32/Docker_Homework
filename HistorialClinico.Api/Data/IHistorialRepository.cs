using HistorialClinico.Api.Dtos;
using HistorialClinico.Api.Models;

namespace HistorialClinico.Api.Data;

public interface IHistorialRepository
{
    Task<IReadOnlyList<Models.HistorialClinico>> GetAllAsync(CancellationToken ct);
    Task<Models.HistorialClinico?> GetByIdAsync(int id, CancellationToken ct);
    Task<Models.HistorialClinico?> GetAutomaticByPatientIdAsync(int patientId, CancellationToken ct);
    Task<Models.HistorialClinico> CreateAsync(HistorialRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, HistorialRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
