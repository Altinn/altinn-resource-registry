using Altinn.TestSeed.Extensions;
using Npgsql;

namespace Altinn.TestSeed;

/// <summary>
/// DB batch builder.
/// </summary>
public class BatchBuilder
{
    private readonly NpgsqlBatch _batch;

    /// <summary>
    /// Constructs a new instance of <see cref="BatchBuilder"/>.
    /// </summary>
    /// <param name="batch">The <see cref="NpgsqlBatch"/>.</param>
    public BatchBuilder(NpgsqlBatch batch)
    {
        _batch = batch;
    }

    /// <inheritdoc cref="NpgsqlBatch.CreateBatchCommand()"/>
    /// <param name="command">The command text.</param>
    public NpgsqlBatchCommand CreateBatchCommand(string command)
        => _batch.CreateBatchCommand(command);
}
