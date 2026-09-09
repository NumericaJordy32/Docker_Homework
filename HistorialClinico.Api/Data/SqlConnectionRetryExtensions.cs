using Microsoft.Data.SqlClient;

namespace HistorialClinico.Api.Data;

internal static class SqlConnectionRetryExtensions
{
    private const int MaxAttempts = 3;

    public static async Task OpenWithRetryAsync(this SqlConnection connection, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (SqlException) when (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }
}
