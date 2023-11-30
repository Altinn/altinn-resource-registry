using System.Data;
using Altinn.ResourceRegistry.Core.PartyRegistry;
using Altinn.ResourceRegistry.Persistence.Aggregates;
using Altinn.ResourceRegistry.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// Repository for party registries.
/// </summary>
internal class PartyRegistryRepository 
    : IPartyRegistryRepository
{
    private readonly NpgsqlDataSource _conn;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyRegistryRepository"/> class
    /// </summary>
    /// <param name="conn">The database connection</param>
    /// <param name="logger">Logger</param>
    /// <param name="timeProvider">Time provider</param>
    public PartyRegistryRepository(
        NpgsqlDataSource conn,
        ILogger<ResourceRegistryRepository> logger,
        TimeProvider timeProvider)
    {
        _conn = conn;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public Task<PartyRegistryInfo> CreatePartyRegistry(string registryOwner, string identifier, string name, string description, CancellationToken cancellationToken = default) 
        => InTransaction(tx => tx.CreatePartyRegistry(registryOwner, identifier, name, description, cancellationToken), cancellationToken);

    private Task<T> InTransaction<T>(Func<TransactionalRepository, Task<T>> func, CancellationToken cancellationToken)
        => TransactionalRepository.RunInTransaction(this, func, cancellationToken);

    private class TransactionalRepository
    {
        public static async Task<T> RunInTransaction<T>(
            PartyRegistryRepository repo,
            Func<TransactionalRepository, Task<T>> func,
            CancellationToken cancellationToken)
        {
            await using var conn = await repo._conn.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var result = await func(new TransactionalRepository(repo._logger, conn, repo._timeProvider));
            await tx.CommitAsync(cancellationToken);

            return result;
        }

        private readonly ILogger _logger;
        private readonly NpgsqlConnection _conn;
        private readonly TimeProvider _timeProvider;

        private TransactionalRepository(ILogger logger, NpgsqlConnection conn, TimeProvider timeProvider)
        {
            _logger = logger;
            _conn = conn;
            _timeProvider = timeProvider;
        }

        public async Task<PartyRegistryInfo> CreatePartyRegistry(string registryOwner, string identifier, string name, string description, CancellationToken cancellationToken)
        {
            // Step 1. Check that the registry doesn't already exist
            var existingRegistry = await Lookup(registryOwner, identifier, cancellationToken);
            if (existingRegistry is not null)
            {
                throw new ArgumentException($"A registry with identifier '{identifier}' already exists for owner '{registryOwner}'.");
            }

            // Step 2. Create a new aggregate.
            var partyRegistryAggregate = PartyRegistryAggregate.New(Guid.NewGuid());
            partyRegistryAggregate.Initialize(_timeProvider.GetUtcNow(), registryOwner, identifier, name, description);

            // Step 3. Apply the event(s) to the database.
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info.
            return partyRegistryAggregate.AsRegistryInfo();
        }

        public async Task<PartyRegistryInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT rid, identifier, registry_owner, registry_name, registry_description, created, modified
                FROM resourceregistry.party_registry_state
                WHERE registry_owner = @owner AND identifier = @identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, registryOwner);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);

            return await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreatePartyRegistryInfo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task ApplyChanges(PartyRegistryAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.party_registry_events (etime, kind, rid, identifier, registry_name, registry_description, registry_owner, actions, party_ids)
                VALUES (@etime, @kind, @rid, @identifier, @registry_name, @registry_description, @registry_owner, @actions, @party_ids);";

            // Step 1. Insert all events into the database.
            {
                await using var cmd = _conn.CreateCommand(QUERY);
                var etime = cmd.Parameters.Add("etime", NpgsqlDbType.TimestampTz);
                var kind = cmd.Parameters.Add("kind", NpgsqlDbType.Text);
                var rid = cmd.Parameters.Add("rid", NpgsqlDbType.Uuid);
                var identifier = cmd.Parameters.Add("identifier", NpgsqlDbType.Text);
                var registry_name = cmd.Parameters.Add("registry_name", NpgsqlDbType.Text);
                var registry_description = cmd.Parameters.Add("registry_description", NpgsqlDbType.Text);
                var registry_owner = cmd.Parameters.Add("registry_owner", NpgsqlDbType.Text);
                var actions = cmd.Parameters.Add("actions", NpgsqlDbType.Array | NpgsqlDbType.Text);
                var party_ids = cmd.Parameters.Add("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid);

                foreach (var evt in aggregate.GetUncommittedEvents())
                {
                    var values = evt.AsValues();
                    etime.SetValue(values.EventTime);
                    kind.SetValue(values.Kind);
                    rid.SetValue(values.AggregateId);
                    identifier.SetNullableValue(values.Identifier);
                    registry_name.SetNullableValue(values.Name);
                    registry_description.SetNullableValue(values.Description);
                    registry_owner.SetNullableValue(values.RegistryOwner);
                    actions.SetNullableValue(values.Actions);
                    party_ids.SetNullableValue(values.PartyIds);

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            // Step 2. Update state tables in the database.
            foreach (var evt in aggregate.GetUncommittedEvents())
            {
                var applyTask = evt switch
                {
                    PartyRegistryCreatedEvent create => ApplyCreateState(create, cancellationToken),
                    PartyRegistryUpdatedEvent update => ApplyUpdateState(update, cancellationToken),
                    PartyRegistryDeletedEvent delete => ApplyDeleteState(delete, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown event type '{evt.GetType().Name}'")
                };

                await applyTask;
            }

            // Step 3. Update modified timestamp on the registry state table.
            if (!aggregate.IsDeleted)
            {
                await UpdateModifiedAt(aggregate, cancellationToken);
            }

            // Step 4. Mark all events as commited.
            aggregate.Commit();
        }

        private async Task ApplyCreateState(PartyRegistryCreatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.party_registry_state
                (rid, identifier, registry_owner, registry_name, created, modified)
                VALUES(@rid, @identifier, @registry_owner, @registry_name, @created, @modified);";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithValue("registry_owner", NpgsqlDbType.Text, evt.RegistryOwner);
            cmd.Parameters.AddWithValue("registry_name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTz, evt.EventTime);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyUpdateState(PartyRegistryUpdatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.party_registry_state
                SET identifier = COALESCE(@identifier, identifier),
                    registry_name = COALESCE(@registry_name, registry_name),
                    registry_description = COALESCE(@registry_description, registry_description),
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithNullableValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithNullableValue("registry_name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithNullableValue("registry_description", NpgsqlDbType.Text, evt.Description);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyDeleteState(PartyRegistryDeletedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.party_registry_state
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task UpdateModifiedAt(PartyRegistryAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.party_registry_state
                SET modified = @modified
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, aggregate.Id);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, aggregate.UpdatedAt);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private static PartyRegistryInfo CreatePartyRegistryInfo(
            NpgsqlDataReader reader)
        {
            var rid = reader.GetGuid("rid");
            var identifier = reader.GetString("identifier");
            var owner = reader.GetString("registry_owner");
            var name = reader.GetString("registry_name");
            var description = reader.GetString("registry_description");
            var created = reader.GetFieldValue<DateTimeOffset>("created");
            var modified = reader.GetFieldValue<DateTimeOffset>("modified");

            return new PartyRegistryInfo(rid, owner, identifier, name, description, created, modified);
        }
    }
}
