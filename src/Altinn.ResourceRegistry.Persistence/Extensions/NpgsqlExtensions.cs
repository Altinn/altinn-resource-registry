using System.Runtime.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.ResourceRegistry.Persistence.Extensions;

/// <summary>
/// Helper extensions for Npgsql.
/// </summary>
internal static class NpgsqlExtensions
{
    /// <summary>
    /// Create a <see cref="NpgsqlCommand"/> with the command text set.
    /// </summary>
    /// <param name="conn">The <see cref="NpgsqlConnection"/>.</param>
    /// <param name="sql">The command text as a string.</param>
    /// <returns>A <see cref="NpgsqlCommand"/>.</returns>
    public static NpgsqlCommand CreateCommand(this NpgsqlConnection conn, string sql)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    /// <summary>
    /// Executes a command against the database, returning a <see cref="IAsyncEnumerable{T}"/>
    /// that can be easily mapped over.
    /// </summary>
    /// <param name="cmd">The <see cref="NpgsqlCommand"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    public static async IAsyncEnumerable<NpgsqlDataReader> ExecuteEnumerableAsync(
        this NpgsqlCommand cmd,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return reader;
        }
    }

    /// <summary>
    /// Adds a <see cref="NpgsqlParameter"/> to the <see cref="NpgsqlParameterCollection"/> given the parameter name and the data type
    /// and value.
    /// </summary>
    /// <param name="parameters">The <see cref="NpgsqlParameterCollection"/> to add the parameter to.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">One of the <see cref="NpgsqlDbType"/> values.</param>
    /// <param name="value">The parameter value. If this is <see langword="null"/>, <see cref="DBNull.Value"/> is used instead.</param>
    /// <returns>The index of the new <see cref="NpgsqlParameter"/> object.</returns>
    public static NpgsqlParameter AddWithNullableValue(
        this NpgsqlParameterCollection parameters,
        string parameterName,
        NpgsqlDbType parameterType,
        object? value)
        => parameters.Add(parameterName, parameterType).SetNullableValue(value);

    /// <summary>
    /// Sets the value of the <see cref="NpgsqlParameter"/> to the given value.
    /// </summary>
    /// <param name="parameter">The <see cref="NpgsqlParameter"/></param>
    /// <param name="value">The new value</param>
    /// <returns><paramref name="parameter"/></returns>
    public static NpgsqlParameter SetValue(
        this NpgsqlParameter parameter,
        object value)
    {
        parameter.Value = value;

        return parameter;
    }

    /// <summary>
    /// Sets the value of the <see cref="NpgsqlParameter"/> to the given value, converting <see langword="null"/>
    /// to <see cref="DBNull.Value"/>.
    /// </summary>
    /// <param name="parameter">The <see cref="NpgsqlParameter"/></param>
    /// <param name="value">The new value</param>
    /// <returns><paramref name="parameter"/></returns>
    public static NpgsqlParameter SetNullableValue(
        this NpgsqlParameter parameter,
        object? value)
    {
        parameter.Value = value;
        if (value is null)
        {
            parameter.Value = DBNull.Value;
        }

        return parameter;
    }
}