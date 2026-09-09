using System.Data;
using HistorialClinico.Api.Dtos;
using Microsoft.Data.SqlClient;
using Historial = HistorialClinico.Api.Models.HistorialClinico;

namespace HistorialClinico.Api.Data;

public sealed class HistorialRepository(IConfiguration configuration) : IHistorialRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("HistorialClinicoDB")
        ?? throw new InvalidOperationException("No se configuró ConnectionStrings:HistorialClinicoDB.");

    public async Task<IReadOnlyList<Historial>> GetAllAsync(CancellationToken ct)
    {
        var result = new List<Historial>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("SELECT IdHistorialClinico, IdPaciente, NumHistoria, Diagnostico, Tratamiento, Fecha FROM HistorialClinico ORDER BY IdHistorialClinico", connection);
        await connection.OpenWithRetryAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(Map(reader));
        return result;
    }

    public async Task<Historial?> GetByIdAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("SELECT IdHistorialClinico, IdPaciente, NumHistoria, Diagnostico, Tratamiento, Fecha FROM HistorialClinico WHERE IdHistorialClinico=@Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenWithRetryAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Historial?> GetAutomaticByPatientIdAsync(int patientId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("SELECT IdHistorialClinico, IdPaciente, NumHistoria, Diagnostico, Tratamiento, Fecha FROM HistorialClinico WHERE NumHistoria=@NumHistoria", connection);
        command.Parameters.Add("@NumHistoria", SqlDbType.VarChar, 30).Value = $"AUTO-{patientId:D8}";
        await connection.OpenWithRetryAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Historial> CreateAsync(HistorialRequest request, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO HistorialClinico (IdPaciente, NumHistoria, Diagnostico, Tratamiento, Fecha)
            OUTPUT INSERTED.IdHistorialClinico, INSERTED.IdPaciente, INSERTED.NumHistoria, INSERTED.Diagnostico, INSERTED.Tratamiento, INSERTED.Fecha
            VALUES (@IdPaciente, @NumHistoria, @Diagnostico, @Tratamiento, COALESCE(@Fecha, GETDATE()));
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, request);
        await connection.OpenWithRetryAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Map(reader);
    }

    public async Task<bool> UpdateAsync(int id, HistorialRequest request, CancellationToken ct)
    {
        const string sql = "UPDATE HistorialClinico SET IdPaciente=@IdPaciente, NumHistoria=@NumHistoria, Diagnostico=@Diagnostico, Tratamiento=@Tratamiento, Fecha=COALESCE(@Fecha, Fecha) WHERE IdHistorialClinico=@Id";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, request);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenWithRetryAsync(ct);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("DELETE FROM HistorialClinico WHERE IdHistorialClinico=@Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static void AddParameters(SqlCommand command, HistorialRequest request)
    {
        command.Parameters.Add("@IdPaciente", SqlDbType.Int).Value = request.IdPaciente;
        command.Parameters.Add("@NumHistoria", SqlDbType.VarChar, 30).Value = request.NumHistoria.Trim();
        command.Parameters.Add("@Diagnostico", SqlDbType.VarChar, 500).Value = request.Diagnostico.Trim();
        command.Parameters.Add("@Tratamiento", SqlDbType.VarChar, 500).Value = (object?)request.Tratamiento?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@Fecha", SqlDbType.DateTime2).Value = (object?)request.Fecha ?? DBNull.Value;
    }

    private static Historial Map(SqlDataReader reader) => new()
    {
        IdHistorialClinico = reader.GetInt32(0), IdPaciente = reader.GetInt32(1), NumHistoria = reader.GetString(2),
        Diagnostico = reader.GetString(3), Tratamiento = reader.IsDBNull(4) ? null : reader.GetString(4), Fecha = reader.GetDateTime(5)
    };
}
