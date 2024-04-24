#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.Urn.Swagger;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// An organization number.
/// </summary>
[DebuggerDisplay("{Value}")]
public record OrganizationNumber
    : IParsable<OrganizationNumber>
    , ISpanParsable<OrganizationNumber>
    , IExampleStringProvider<OrganizationNumber>
{
    private static readonly SearchValues<char> NUMBERS = SearchValues.Create(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);

    /// <summary>
    /// The organization number as a string.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc/>
    public static string ExampleString => "123456789";

    private OrganizationNumber(string value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    public static OrganizationNumber Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
            ? result
            : throw new FormatException("Invalid OrganizationNumber");

    /// <inheritdoc/>
    public static OrganizationNumber Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
            ? result
            : throw new FormatException("Invalid OrganizationNumber");

    /// <inheritdoc/>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out OrganizationNumber result)
        => TryParse(s.AsSpan(), provider, s, out result);

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out OrganizationNumber result)
        => TryParse(s, provider, null, out result);

    private static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, string? original, [MaybeNullWhen(false)] out OrganizationNumber result)
    {
        if (s.Length != 9)
        {
            result = null;
            return false;
        }

        if (s.ContainsAnyExcept(NUMBERS))
        {
            result = null;
            return false;
        }

        result = new OrganizationNumber(original ?? new string(s));
        return true;
    }
}
