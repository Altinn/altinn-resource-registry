using Npgsql;

namespace Altinn.ServiceDefaults.Npgsql;

public interface INpgsqlConnectionProvider
{
    string ConnectionString { get; }

    Task<NpgsqlConnection> GetConnection(CancellationToken cancellationToken);
}

internal class TempSharedNonPooledNpgsqlConnectionProvider
    : INpgsqlConnectionProvider
    , IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly Lazy<Task<NpgsqlConnection>> _connection;
    private readonly CancellationTokenSource _cts = new();

    public TempSharedNonPooledNpgsqlConnectionProvider(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = false,
        };

        connectionString = builder.ConnectionString;
        _connectionString = connectionString;

        var token = _cts.Token;
        _connection = new(() => OpenConnection(connectionString, token), LazyThreadSafetyMode.ExecutionAndPublication);

        static async Task<NpgsqlConnection> OpenConnection(string connectionString, CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    public string ConnectionString => _connectionString;

    public Task<NpgsqlConnection> GetConnection(CancellationToken cancellationToken)
        => _connection.Value;

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _cts.Dispose();
        if (_connection.IsValueCreated)
        {
            var task = _connection.Value;
            
            return task.IsCompletedSuccessfully 
                ? task.Result.DisposeAsync()
                : DisposeWhenReady(task);
        }

        return ValueTask.CompletedTask;

        static async ValueTask DisposeWhenReady(Task<NpgsqlConnection> connection)
        {
            await using var conn = await connection;
        }
    }
}
