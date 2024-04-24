using System.Diagnostics;
using System.Numerics;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseCreator;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly record struct DatabaseCreationOrder
    : IEquatable<DatabaseCreationOrder>
    , IComparable<DatabaseCreationOrder>
    , IEqualityOperators<DatabaseCreationOrder, DatabaseCreationOrder, bool>
    , IComparisonOperators<DatabaseCreationOrder, DatabaseCreationOrder, bool>
{
    public static readonly DatabaseCreationOrder CreateDatabases = new(0); // default
    public static readonly DatabaseCreationOrder CreateRoles = new(-10); // before databases
    public static readonly DatabaseCreationOrder CreateGrants = new(10); // after databases

    private readonly int _value;

    private DatabaseCreationOrder(int value)
    {
        _value = value;
    }

    public int CompareTo(DatabaseCreationOrder other)
        => _value.CompareTo(other._value);

    public bool Equals(DatabaseCreationOrder other)
        => _value == other._value;

    public override int GetHashCode()
        => _value.GetHashCode();

    public static bool operator <(DatabaseCreationOrder left, DatabaseCreationOrder right) 
        => left.CompareTo(right) < 0;

    public static bool operator >(DatabaseCreationOrder left, DatabaseCreationOrder right) 
        => left.CompareTo(right) > 0;

    public static bool operator <=(DatabaseCreationOrder left, DatabaseCreationOrder right) 
        => left.CompareTo(right) <= 0;

    public static bool operator >=(DatabaseCreationOrder left, DatabaseCreationOrder right) 
        => left.CompareTo(right) >= 0;
}
