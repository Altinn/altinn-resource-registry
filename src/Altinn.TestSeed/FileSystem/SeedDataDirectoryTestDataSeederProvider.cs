using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Altinn.TestSeed.Extensions;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.TestSeed.FileSystem;

/// <summary>
/// A <see cref="ITestDataSeederProvider"/> that provides seeders from a directory.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal partial class SeedDataDirectoryTestDataSeederProvider
    : ITestDataSeederProvider
{
    private readonly ILogger<SeedDataDirectoryTestDataSeederProvider> _logger;
    private readonly IFileProvider _fileProvider;
    private readonly SeedDataDirectorySettings _settings;

    /// <summary>
    /// Constructs a new instance of <see cref="SeedDataDirectoryTestDataSeederProvider"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settings">The settings.</param>
    public SeedDataDirectoryTestDataSeederProvider(
        ILogger<SeedDataDirectoryTestDataSeederProvider> logger,
        SeedDataDirectorySettings settings)
    {
        _logger = logger;
        _settings = settings;

        if (_settings.FileProvider is null)
        {
            ThrowHelper.ThrowArgumentException(nameof(settings), "The file provider must be set.");
        }

        _fileProvider = _settings.FileProvider;
        var rootDir = _fileProvider.GetFileInfo("/");
        DisplayName = $"Directory: {rootDir.PhysicalPath ?? rootDir.Name}";
    }

    /// <inheritdoc/>
    public string DisplayName { get; }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ITestDataSeeder> GetSeeders(NpgsqlConnection db, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var entry in _fileProvider.GetDirectoryContents("/"))
        {
            if (entry.IsDirectory)
            {
                await foreach (var seeder in GetSeedersFromDirectory("/", entry, db, cancellationToken))
                {
                    yield return seeder;
                }
            } 
            else if (await TryGetSeederFromFile(entry, db, dirOrder: null, cancellationToken) is { } seeder)
            {
                yield return seeder;
            }
        }
    }

    private IAsyncEnumerable<ITestDataSeeder> GetSeedersFromDirectory(string prefix, IFileInfo dir, NpgsqlConnection db, CancellationToken cancellationToken)
    {
        if (!TryParseFileName(dir.Name, out var dirOrder, out var tableNameSpan))
        {
            Log.InvalidDirectoryName(_logger, dir.PhysicalPath ?? dir.Name);
            throw new InvalidOperationException($"Invalid directory name: {dir.Name}");
        }

        string? tableName = tableNameSpan.IsEmpty ? null : new string(tableNameSpan);
        return ReadSeedersFromDirectory(prefix, dir, tableName, dirOrder, db, cancellationToken);
    }

    private async IAsyncEnumerable<ITestDataSeeder> ReadSeedersFromDirectory(
        string prefix, 
        IFileInfo dir,
        string? tableName,
        byte? dirOrder,
        NpgsqlConnection db,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (tableName is not null)
        {
            var isEmpty = await IsTableEmpty(db, tableName, cancellationToken);

            if (!isEmpty)
            {
                Log.SkippingDirectory(_logger, dir.PhysicalPath ?? dir.Name, "table not empty");
                yield break;
            }
        }

        var path = $"{prefix}{dir.Name}";
        var checkPath = $"{path}/_check.sql";
        var file = _fileProvider.GetFileInfo(checkPath);
        if (file.Exists)
        {
            await using var fs = file.CreateReadStream();
            using var reader = new StreamReader(fs, encoding: Encoding.UTF8, leaveOpen: true);
            var data = await reader.ReadToEndAsync(cancellationToken);

            await using var cmd = db.CreateCommand(data);
            await using var dbReader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await dbReader.ReadAsync(cancellationToken))
            {
                // we expect a single boolean result.
                var result = dbReader.GetBoolean(0);
                if (!result)
                {
                    // if we get false, we skip the directory.
                    Log.SkippingDirectory(_logger, dir.PhysicalPath ?? dir.Name, "check returned false");
                    yield break;
                }
            }
        }

        foreach (var entry in _fileProvider.GetDirectoryContents(path))
        {
            if (entry.IsDirectory)
            {
                var subPath = $"{path}/{entry.Name}";
                ThrowHelper.ThrowInvalidOperationException($"Nested directories are not supported: {entry.PhysicalPath ?? subPath}.");
            }

            var name = entry.Name;
            switch (name)
            {
                case "_check.sql":
                    continue;

                case "_prepare.sql":
                    yield return new SeedDataFileTestDataSeeder(
                        _logger,
                        entry,
                        new SeedDataFileOrder
                        {
                            DirectoryOrder = dirOrder,
                            Type = SeedDataFileOrder.SeedDataFileType.PrepareScript,
                        });
                    break;

                case "_finalize.sql":
                    yield return new SeedDataFileTestDataSeeder(
                        _logger,
                        entry,
                        new SeedDataFileOrder
                        {
                            DirectoryOrder = dirOrder,
                            Type = SeedDataFileOrder.SeedDataFileType.FinalizeScript,
                        });
                    break;

                default:
                    if (await TryGetSeederFromFile(entry, db, dirOrder, cancellationToken) is { } seeder)
                    {
                        yield return seeder;
                    }

                    break;
            }
        }
    }

    private ValueTask<ITestDataSeeder?> TryGetSeederFromFile(IFileInfo file, NpgsqlConnection db, byte? dirOrder, CancellationToken cancellationToken)
    {
        var name = file.Name.AsSpan();
        var withoutExtension = RemoveExtension(name);
        
        if (!TryParseFileName(withoutExtension, out var orderNumber, out var table))
        {
            Log.InvalidFileName(_logger, file.PhysicalPath ?? file.Name);
            throw new InvalidOperationException($"Invalid file name: {name}");
        }

        if (!name.EndsWith(".sql", StringComparison.Ordinal))
        {
            Log.InvalidFileType(_logger, file.PhysicalPath ?? file.Name);
            throw new InvalidOperationException($"Invalid file type: {name}");
        }

        var order = new SeedDataFileOrder
        {
            FileOrder = orderNumber,
            Type = SeedDataFileOrder.SeedDataFileType.SeedScript,
            DirectoryOrder = dirOrder,
        };
        var seeder = new SeedDataFileTestDataSeeder(_logger, file, order);
        if (!table.IsEmpty)
        {
            var tableName = new string(table);
            return IfTableIsEmpty(tableName, file.PhysicalPath ?? file.Name, db, seeder, this, cancellationToken);
        }

        return ValueTask.FromResult<ITestDataSeeder?>(seeder);

        static async ValueTask<ITestDataSeeder?> IfTableIsEmpty(
            string tableName, 
            string fileName,
            NpgsqlConnection db, 
            ITestDataSeeder seeder, 
            SeedDataDirectoryTestDataSeederProvider self, 
            CancellationToken cancellationToken)
        {
            var isEmpty = await self.IsTableEmpty(db, tableName, cancellationToken);

            if (!isEmpty)
            {
                Log.SkippingFile(self._logger, fileName, "table not empty");
                return null;
            }

            return seeder;
        }
    }

    private async Task<bool> IsTableEmpty(NpgsqlConnection db, string tableName, CancellationToken cancellationToken)
    {
        Log.CheckIfTableIsEmpty(_logger, tableName);

        var query = $"SELECT 1 FROM {tableName} LIMIT 1";
        await using var cmd = db.CreateCommand(query);

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);
        var hasData = await reader.ReadAsync(cancellationToken);

        Log.TableHasData(_logger, tableName, hasData);
        return !hasData;
    }

    private bool TryParseFileName(ReadOnlySpan<char> fileNameWithoutSuffix, out byte? order, out ReadOnlySpan<char> table)
    {
        order = null;
        table = default;

        var fileName = fileNameWithoutSuffix;
        if (fileName.Length == 0)
        {
            return false;
        }

        // file starts with a number.
        if (char.IsAsciiDigit(fileName[0]))
        {
            // if file starts with a number, we require the format to be ##-<name>.
            if (fileName.Length < 3)
            {
                return false;
            }

            if (!char.IsAsciiDigit(fileName[1]))
            {
                return false;
            }

            if (fileName[2] != '-')
            {
                return false;
            }

            order = byte.Parse(fileName[..2], NumberStyles.None);
            fileName = fileName[3..];
        }

        if (fileName.Length == 0)
        {
            return false;
        }

        // file is _<name> - this is not allowed as names starting with _ are reserved.
        if (fileName[0] == '_')
        {
            return false;
        }

        // file is [table-name]
        if (fileName[0] == '[')
        {
            // if file-name starts with a [, we require the format to be [<name>].
            if (fileName[^1] != ']')
            {
                return false;
            }

            if (fileName.Length < 3)
            {
                return false;
            }

            table = fileName[1..^1];
        }

        return true;
    }

    private static ReadOnlySpan<char> RemoveExtension(ReadOnlySpan<char> fileName)
    {
        var lastDot = fileName.LastIndexOf('.');
        return lastDot == -1 ? fileName : fileName[..lastDot];
    }

    private static ReadOnlySpan<char> DirName(ReadOnlySpan<char> path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash == -1 ? path : path[..lastSlash];
    }

    private string DebuggerDisplay => DisplayName;

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Error, "Invalid file name: {FileName}.")]
        public static partial void InvalidFileName(ILogger logger, string fileName);

        [LoggerMessage(2, LogLevel.Error, "Invalid file type: {FileName}.")]
        public static partial void InvalidFileType(ILogger logger, string fileName);

        [LoggerMessage(3, LogLevel.Debug, "Checking if table {TableName} is empty.")]
        public static partial void CheckIfTableIsEmpty(ILogger logger, string tableName);

        [LoggerMessage(4, LogLevel.Debug, "Table {TableName} has data: {HasData}.")]
        public static partial void TableHasData(ILogger logger, string tableName, bool hasData);

        [LoggerMessage(5, LogLevel.Debug, "Seeding data from file: {FileName}.")]
        public static partial void SeedData(ILogger logger, string fileName);

        [LoggerMessage(6, LogLevel.Error, "Invalid directory name: {DirectoryName}.")]
        public static partial void InvalidDirectoryName(ILogger logger, string directoryName);

        [LoggerMessage(7, LogLevel.Debug, "Skipping directory {DirectoryName} because {Reason}.")]
        public static partial void SkippingDirectory(ILogger logger, string directoryName, string reason);

        [LoggerMessage(8, LogLevel.Debug, "Skipping file {FileName} because {Reason}.")]
        public static partial void SkippingFile(ILogger logger, string fileName, string reason);
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private sealed class SeedDataFileTestDataSeeder
    : ITestDataSeeder
    {
        private readonly ILogger _logger;
        private readonly IFileInfo _file;
        private readonly SeedDataFileOrder _order;

        public SeedDataFileTestDataSeeder(
            ILogger<SeedDataDirectoryTestDataSeederProvider> logger, 
            IFileInfo file, 
            SeedDataFileOrder order)
        {
            _logger = logger;
            _file = file;
            _order = order;
        }

        public uint Order => _order;

        public string DisplayName => $"File: {_file.PhysicalPath ?? _file.Name}";

        public async Task SeedData(BatchBuilder batch, CancellationToken cancellationToken)
        {
            Log.SeedData(_logger, _file.PhysicalPath ?? _file.Name);
            await using var fs = _file.CreateReadStream();
            using var reader = new StreamReader(fs, encoding: Encoding.UTF8, leaveOpen: true);
            var data = await reader.ReadToEndAsync(cancellationToken);

            SplitAndAddQuery(batch, data);
        }

        private static void SplitAndAddQuery(BatchBuilder batch, string data)
        {
            var splitter = new QuerySplitter(data.AsSpan());

            foreach (var query in splitter)
            {
                batch.CreateBatchCommand(query);
            }
        }

        private string DebuggerDisplay => DisplayName;
    }
}
