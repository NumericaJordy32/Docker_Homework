using System.Data;
using Microsoft.Data.SqlClient;
using Pacientes.Api.Dtos;
using Pacientes.Api.Models;

namespace Pacientes.Api.Data;

public sealed class PacienteRepository(IConfiguration configuration) : IPacienteRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("PacientesDB")
        ?? throw new InvalidOperationException("No se configuró ConnectionStrings:PacientesDB.");

    public async Task<IReadOnlyList<Paciente>> GetAllAsync(CancellationToken ct)
    {
        var result = new List<Paciente>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("SELECT IdPaciente, Cedula, Nombre, Apellido, Direccion FROM Paciente ORDER BY IdPaciente", connection);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(Map(reader));
        return result;
    }

    public async Task<Paciente?> GetByIdAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("SELECT IdPaciente, Cedula, Nombre, Apellido, Direccion FROM Paciente WHERE IdPaciente=@Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Paciente> CreateAsync(PacienteRequest request, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Paciente (Cedula, Nombre, Apellido, Direccion)
            OUTPUT INSERTED.IdPaciente, INSERTED.Cedula, INSERTED.Nombre, INSERTED.Apellido, INSERTED.Direccion
            VALUES (@Cedula, @Nombre, @Apellido, @Direccion);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, request);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Map(reader);
    }

    public async Task<bool> UpdateAsync(int id, PacienteRequest request, CancellationToken ct)
    {
        const string sql = "UPDATE Paciente SET Cedula=@Cedula, Nombre=@Nombre, Apellido=@Apellido, Direccion=@Direccion WHERE IdPaciente=@Id";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, request);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("DELETE FROM Paciente WHERE IdPaciente=@Id", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(ct);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static void AddParameters(SqlCommand command, PacienteRequest request)
    {
        command.Parameters.Add("@Cedula", SqlDbType.VarChar, 20).Value = request.Cedula.Trim();
        command.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value = request.Nombre.Trim();
        command.Parameters.Add("@Apellido", SqlDbType.VarChar, 100).Value = request.Apellido.Trim();
        command.Parameters.Add("@Direccion", SqlDbType.VarChar, 250).Value = (object?)request.Direccion?.Trim() ?? DBNull.Value;
    }

    private static Paciente Map(SqlDataReader reader) => new()
    {
        IdPaciente = reader.GetInt32(0), Cedula = reader.GetString(1), Nombre = reader.GetString(2),
        Apellido = reader.GetString(3), Direccion = reader.IsDBNull(4) ? null : reader.GetString(4)
    };
}
