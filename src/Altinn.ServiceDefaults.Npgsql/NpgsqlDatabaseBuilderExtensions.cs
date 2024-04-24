using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.ServiceDefaults.Npgsql;

public static class NpgsqlDatabaseBuilderExtensions 
{ 
    public static INpgsqlDatabaseBuilder Configure(this INpgsqlDatabaseBuilder builder, Action<NpgsqlDataSourceBuilder> configure)
    {
        if (builder.ServiceKey is not null)
        {
            builder.Services.AddKeyedSingleton<IConfigureOptions<NpgsqlDataSourceBuilder>>(builder.ServiceKey, new ConfigureOptions<NpgsqlDataSourceBuilder>(configure));
        }
        else
        {
            builder.Services.AddSingleton<IConfigureOptions<NpgsqlDataSourceBuilder>>(new ConfigureOptions<NpgsqlDataSourceBuilder>(configure));
        }

        return builder;
    }

    public static INpgsqlDatabaseBuilder MapEnum<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(
        this INpgsqlDatabaseBuilder builder, 
        string? pgName = null, 
        INpgsqlNameTranslator? nameTranslator = null)
        where TEnum: struct, Enum
    {
        return builder.Configure(b => b.MapEnum<TEnum>(pgName, nameTranslator));
    }
}
