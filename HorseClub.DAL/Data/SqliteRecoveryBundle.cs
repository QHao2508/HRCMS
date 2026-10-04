using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Horse_BackEnd.Data;

public static class SqliteRecoveryBundle
{
    public sealed record Entry(string Path, long Length, string Sha256);
    public sealed record Manifest(int Version, string Provider, DateTimeOffset CreatedAt, List<Entry> Files);

    public static void Create(string database, string uploads, string keys, string destination)
    {
        database = Path.GetFullPath(database); uploads = Path.GetFullPath(uploads); keys = Path.GetFullPath(keys); destination = Path.GetFullPath(destination);
        if (!File.Exists(database) || !Directory.Exists(uploads) || !Directory.Exists(keys)) throw new IOException("Database, uploads and keys must all exist.");
        foreach (var source in new[] { database, uploads, keys })
            if (Inside(source, destination) || Inside(destination, source)) throw new IOException("Backup destination must be separate from source data.");
        RejectLink(database); NewDestination(destination);
        using (var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ConnectionString))
        using (var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(destination, "database.db"), Pooling = false }.ConnectionString))
        {
            source.Open(); target.Open(); source.BackupDatabase(target);
            // A source in WAL mode transfers that flag. Make the snapshot a standalone file
            // before hashing so read-only verification never introduces -wal/-shm sidecars.
            using var standalone = target.CreateCommand(); standalone.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals((string?)standalone.ExecuteScalar(), "delete", StringComparison.OrdinalIgnoreCase)) throw new IOException("Could not make standalone SQLite snapshot.");
        }
        CopyTree(uploads, Path.Combine(destination, "uploads"));
        CopyTree(keys, Path.Combine(destination, "keys"));
        var entries = Files(destination).Select(path => new Entry(Path.GetRelativePath(destination, path).Replace('\\', '/'), new FileInfo(path).Length, Hash(path))).OrderBy(x => x.Path, StringComparer.Ordinal).ToList();
        File.WriteAllText(Path.Combine(destination, "manifest.json"), JsonSerializer.Serialize(new Manifest(1, "Sqlite", DateTimeOffset.UtcNow, entries), new JsonSerializerOptions { WriteIndented = true }));
        Verify(destination);
    }

    public static void Verify(string bundle)
    {
        bundle = Path.GetFullPath(bundle); RejectLink(bundle);
        var manifestPath = Path.Combine(bundle, "manifest.json"); RejectLink(manifestPath);
        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath)) ?? throw new IOException("Invalid manifest.");
        if (manifest.Version != 1 || manifest.Provider != "Sqlite" || manifest.Files is null) throw new IOException("Unsupported bundle.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(entry.Path) || entry.Path.Contains('\\') || entry.Path.Split('/').Any(x => x is "" or "." or "..")
                || entry.Path.Contains(':') || !paths.Add(entry.Path)
                || !(entry.Path == "database.db" || entry.Path.StartsWith("uploads/", StringComparison.Ordinal) || entry.Path.StartsWith("keys/", StringComparison.Ordinal)))
                throw new IOException("Unsafe or duplicate manifest path.");
            var path = Path.Combine(bundle, entry.Path); RejectLink(path);
            if (!File.Exists(path) || new FileInfo(path).Length != entry.Length || Hash(path) != entry.Sha256) throw new IOException("Bundle integrity check failed.");
        }
        var actual = Files(bundle).Select(x => Path.GetRelativePath(bundle, x).Replace('\\', '/')).Where(x => x != "manifest.json");
        if (!paths.SetEquals(actual) || !paths.Contains("database.db") || !paths.Any(x => x.StartsWith("keys/", StringComparison.Ordinal)))
            throw new IOException("Incomplete bundle or unexpected files.");
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(bundle, "database.db"), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ConnectionString);
        connection.Open();
        using var integrity = connection.CreateCommand(); integrity.CommandText = "PRAGMA integrity_check";
        if ((string?)integrity.ExecuteScalar() != "ok") throw new IOException("SQLite integrity_check failed.");
        using var foreignKeys = connection.CreateCommand(); foreignKeys.CommandText = "PRAGMA foreign_key_check";
        using (var reader = foreignKeys.ExecuteReader()) if (reader.Read()) throw new IOException("SQLite foreign_key_check failed.");
        foreach (var table in new[] { "Attachments", "IncidentPhotos" })
        {
            using var exists = connection.CreateCommand(); exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name"; exists.Parameters.AddWithValue("$name", table);
            if ((long)exists.ExecuteScalar()! == 0) continue;
            using var references = connection.CreateCommand(); references.CommandText = $"SELECT StorageName, Length FROM {table}";
            using var rows = references.ExecuteReader();
            while (rows.Read())
            {
                var name = rows.GetString(0);
                if (name.Length != 32 || name.Any(x => !char.IsAsciiHexDigit(x)) || !paths.Contains("uploads/" + name)
                    || new FileInfo(System.IO.Path.Combine(bundle, "uploads", name)).Length != rows.GetInt64(1))
                    throw new IOException("Database references an invalid or missing uploaded file.");
            }
        }
    }

    public static void Restore(string bundle, string destination)
    {
        Verify(bundle); NewDestination(destination);
        File.Copy(Path.Combine(bundle, "database.db"), Path.Combine(destination, "database.db"));
        CopyTree(Path.Combine(bundle, "uploads"), Path.Combine(destination, "uploads"));
        CopyTree(Path.Combine(bundle, "keys"), Path.Combine(destination, "keys"));
    }

    private static void NewDestination(string path)
    {
        RejectLink(path);
        if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Destination must not already exist. Restore never overwrites data.");
        Directory.CreateDirectory(path);
    }
    private static bool Inside(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    private static void RejectLink(string path)
    {
        for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("Symlinks/junctions are not accepted for recovery files.");
    }
    private static IEnumerable<string> Files(string root)
    {
        RejectLink(root);
        foreach (var file in Directory.GetFiles(root)) { RejectLink(file); yield return file; }
        foreach (var directory in Directory.GetDirectories(root)) foreach (var file in Files(directory)) yield return file;
    }
    private static void CopyTree(string source, string target)
    {
        RejectLink(source); Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) { RejectLink(file); File.Copy(file, Path.Combine(target, Path.GetFileName(file))); }
        foreach (var directory in Directory.GetDirectories(source)) CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
    }
}
