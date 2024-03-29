﻿using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Persistence.Tests;

public abstract class DbTests 
    : IAsyncLifetime
    , IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;
    private DbFixture.OwnedDb? _db;

    protected DbTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    private ServiceProvider? _services;
    private AsyncServiceScope _scope;

    protected IServiceProvider Services => _scope!.ServiceProvider;

    protected virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (_scope is { } scope) await scope.DisposeAsync();
        if (_services is { } services) await services.DisposeAsync();
        if (_db is { } db) await db.DisposeAsync();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        var container = new ServiceCollection();
        container.AddLogging(l => l.AddConsole());
        _db = await _dbFixture.CreateDbAsync();
        _db.ConfigureServices(container);
        ConfigureServices(container);

        _services = container.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        _scope = _services.CreateAsyncScope();
    }
}

