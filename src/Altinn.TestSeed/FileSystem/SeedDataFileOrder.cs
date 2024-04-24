using System.Diagnostics;
using CommunityToolkit.Diagnostics;

namespace Altinn.TestSeed.FileSystem;

/// <summary>
/// Represents how a file from a <see cref="SeedDataDirectoryTestDataSeederProvider"/>
/// should be ordered.
/// </summary>
/// <remarks>
/// Sorting rules (in order of precedence):
/// <list type="number">
///     <item>All <see cref="SeedDataFileType.PrepareScript"/>s run before everything else.</item>
///     <item>All <see cref="SeedDataFileType.FinalizeScript"/>s run after everything else.</item>
///     <item>
///         Files in the root directory counts as if they were unsorted files in a directory with the
///         file's order value. For instance, if you have the file <c>01-foo.sql</c>, it will be ordered
///         as if it were <c>01-foo/foo.sql</c>.
///     </item>
///     <item>Files and directories missing sorting-components are run after those that has it.</item>
/// </list>
/// </remarks>
internal readonly record struct SeedDataFileOrder
{
    private const uint TypeOrderMask = 0x00FF0000;
    private const uint DirectoryOrderMask = 0x0000FF00;
    private const uint FileOrderMask = 0x000000FF;

    private static byte GetTypeOrderByte(uint value)
        => (byte)((value & TypeOrderMask) >> 16);

    private static uint SetTypeOrderByte(uint value, byte byteValue)
        => (value & ~TypeOrderMask) | ((uint)byteValue << 16);

    private static byte GetDirectoryOrderByte(uint value)
        => (byte)((value & DirectoryOrderMask) >> 8);

    private static uint SetDirectoryOrderByte(uint value, byte byteValue)
        => (value & ~DirectoryOrderMask) | ((uint)byteValue << 8);

    private static byte GetFileOrderByte(uint value)
        => (byte)(value & FileOrderMask);

    private static uint SetFileOrderByte(uint value, byte byteValue)
        => (value & ~FileOrderMask) | byteValue;

    // Layout (in bytes):
    // most significant byte: unused (reserved for future use)
    // next byte: type order (sorted first, [1..3])
    // next byte: directory order (sorted second, [0..100])
    // least significant byte: file order (sorted third, [0..100])
    private readonly uint _value;

    /// <summary>
    /// Gets a value indicating whether or not this <see cref="SeedDataFileType"/> is
    /// set.
    /// </summary>
    public bool IsSet => _value != 0;

    /// <summary>
    /// Gets the order value of the directory containing the file,
    /// or <see langword="null"/> if the file is not in a directory
    /// or the containing directory does not have a specified ordering.
    /// </summary>
    public byte? DirectoryOrder
    {
        get
        {
            var byteValue = GetDirectoryOrderByte(_value);
            Debug.Assert(byteValue <= 100, "The directory order value must be less than or equal to 100.");

            return byteValue == 100 ? null : byteValue;
        }

        init
        {
            if (value is not null)
            {
                // 100 is the null-value. IsInRange is exclusive at upper bound.
                Guard.IsInRange(value.Value, (byte)0, (byte)100);
            }
            
            var byteValue = value ?? 100;
            _value = SetDirectoryOrderByte(_value, byteValue);
        }
    }

    /// <summary>
    /// Gets the order value of the file, or <see langword="null"/>
    /// if the file does not have a specified ordering.
    /// </summary>
    public byte? FileOrder
    {
        get
        {
            var byteValue = GetFileOrderByte(_value);
            Debug.Assert(byteValue <= 100, "The file order value must be less than or equal to 100.");

            return byteValue == 0 ? null : byteValue;
        }

        init
        {
            if (value is not null)
            {
                // 0 is the null-value. IsInRange is exclusive at upper bound.
                Guard.IsInRange(value.Value, (byte)0, (byte)100);
            }

            var byteValue = value ?? 0;
            _value = SetFileOrderByte(_value, byteValue);
        }
    }

    /// <summary>
    /// Gets the type of the file.
    /// </summary>
    public SeedDataFileType Type
    {
        get
        {
            var byteValue = GetTypeOrderByte(_value);
            Debug.Assert(byteValue is >= 1 and <= 3, "The type order value must be in the range [1..3].");

            return (SeedDataFileType)byteValue;
        }

        init
        {
            if (!Enum.IsDefined(value))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), "Invalid SeedDataFileType value.");
            }
            
            var byteValue = (byte)value;
            _value = SetTypeOrderByte(_value, byteValue);
        }
    }

    public static implicit operator uint(SeedDataFileOrder order) => order._value;

    /// <summary>
    /// The different file types supported by <see cref="SeedDataFileOrder"/>.
    /// </summary>
    public enum SeedDataFileType : byte
    {
        /// <summary>
        /// A prepare script that runs before everything else.
        /// </summary>
        PrepareScript = 1,

        /// <summary>
        /// A seed data script.
        /// </summary>
        SeedScript = 2,

        /// <summary>
        /// A finalize script that runs after everything else.
        /// </summary>
        FinalizeScript = 3,
    }
}
