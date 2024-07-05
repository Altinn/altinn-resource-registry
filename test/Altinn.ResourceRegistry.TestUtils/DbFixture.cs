﻿using Altinn.Authorization.ServiceDefaults.Npgsql.Yuniql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Altinn.ResourceRegistry.TestUtils;

public class DbFixture 
    : IAsyncLifetime
{
    private const int MAX_CONCURRENCY = 20;

    Singleton.Ref<Inner>? _inner;

    public async Task InitializeAsync()
    {
        _inner = await Singleton.Get<Inner>();
    }

    public Task<OwnedDb> CreateDbAsync()
        => _inner!.Value.CreateDbAsync(this);

    private Task DropDbAsync(OwnedDb ownedDb)
        => _inner!.Value.DropDatabaseAsync(ownedDb);

    public async Task DisposeAsync()
    {
        if (_inner is { } inner)
        {
            await inner.DisposeAsync();
        }
    }

    private class Inner : IAsyncLifetime
    {
        private int _dbCounter = 0;
        private readonly AsyncLock _dbLock = new();
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("docker.io/postgres:16.1-alpine")
            .WithCleanUp(true)
            .Build();

        private readonly AsyncConcurrencyLimiter _throtler = new(MAX_CONCURRENCY);

        string? _connectionString;
        NpgsqlDataSource? _db;

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            _connectionString = _dbContainer.GetConnectionString() + "; Include Error Detail=true; Pooling=false;";
            _db = NpgsqlDataSource.Create(_connectionString);
        }

        public async Task<OwnedDb> CreateDbAsync(DbFixture fixture)
        {
            var dbName = $"test_{Interlocked.Increment(ref _dbCounter)}";

            var ticket = await _throtler.Acquire();

            try
            {
                // only create 1 db at once
                using var guard = await _dbLock.Acquire();

                await using var cmd = _db!.CreateCommand(/*strpsql*/$"CREATE DATABASE {dbName};");
                await cmd.ExecuteNonQueryAsync();

                var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_connectionString) { Database = dbName, IncludeErrorDetail = true };
                var connectionString = connectionStringBuilder.ToString();

                var ownedDb = new OwnedDb(connectionString, dbName, fixture, ticket);
                ticket = null;
                return ownedDb;
            }
            finally
            {
                ticket?.Dispose();
            }
        }

        public async Task DropDatabaseAsync(OwnedDb ownedDb)
        {
            await using var cmd = _db!.CreateCommand(/*strpsql*/$"DROP DATABASE IF EXISTS {ownedDb.DbName};");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            if (_db is { })
            {
                await _db.DisposeAsync();
            }

            await _dbContainer.DisposeAsync();
            _throtler.Dispose();
            _dbLock.Dispose();
        }

        static string FindWorkspace()
        {
            var dir = Environment.CurrentDirectory;
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir, ".git")))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new InvalidOperationException("Workspace directory not found");
        }
    }

    public sealed class OwnedDb : IAsyncDisposable
    {
        readonly string _connectionString;
        readonly string _dbName;
        readonly DbFixture _db;
        readonly IDisposable _ticket;

        public OwnedDb(string connectionString, string dbName, DbFixture db, IDisposable ticket)
        {
            _connectionString = connectionString;
            _dbName = dbName;
            _db = db;
            _ticket = ticket;
        }

        public string ConnectionString => _connectionString;

        internal string DbName => _dbName;

        public void ConfigureApplication(IHostApplicationBuilder builder)
        {
            var serviceDescriptor = builder.GetAltinnServiceDescriptor();
            ConfigureConfiguration(builder.Configuration, serviceDescriptor.Name);
            ConfigureServices(builder.Services, serviceDescriptor.Name);
        }

        public void ConfigureConfiguration(IConfigurationBuilder builder, string serviceName)
        {
            builder.AddInMemoryCollection([
                KeyValuePair.Create<string, string?>($"ConnectionStrings:{serviceName}_db", _connectionString),
            ]);
        }

        public void ConfigureServices(IServiceCollection services, string serviceName)
        {
            services.AddOptions<YuniqlDatabaseMigratorOptions>()
                .Configure(cfg =>
                {
                    cfg.Environment = "integrationtest";
                });
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DropDbAsync(this);
            _ticket.Dispose();
        }
    }
}
