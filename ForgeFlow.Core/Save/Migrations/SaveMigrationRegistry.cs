using System.Reflection;
using System.Text.Json.Nodes;

namespace ForgeFlow.Core.Save.Migrations;

/// <summary>
/// Ordered registry of all save migrations. Migrations are applied sequentially
/// from the save's current version up to <see cref="SaveData.CurrentVersion"/>.
/// <para>
/// Migrations are <b>auto-discovered</b> via reflection over the Core assembly:
/// every non-abstract <see cref="ISaveMigration"/> implementor with a public
/// parameterless constructor is instantiated and linked into the chain by its
/// <see cref="ISaveMigration.FromVersion"/> / <see cref="ISaveMigration.ToVersion"/>.
/// This closes the "forgot to add the migration to the list" bug class — a new
/// <see cref="ISaveMigration"/> class is picked up automatically, and a broken
/// or duplicated chain edge throws loudly from the constructor.
/// </para>
/// <para>
/// Reflection runs once at bootstrap (§7.1 permits reflection at bootstrap,
/// not on the hot path). Discovery is tolerant of
/// <see cref="ReflectionTypeLoadException"/> so partial assembly loads (tests,
/// mod hosting) still work.
/// </para>
/// </summary>
public sealed class SaveMigrationRegistry
{
    private readonly List<ISaveMigration> _migrations;

    public SaveMigrationRegistry()
    {
        _migrations = BuildChain(DiscoverMigrations());
        Validate();
    }

    /// <summary>
    /// Test / diagnostic hook: build a registry from an explicit migration set
    /// without running assembly discovery. Migrations will still be auto-ordered
    /// into a chain and validated.
    /// </summary>
    internal SaveMigrationRegistry(IEnumerable<ISaveMigration> migrations)
    {
        _migrations = BuildChain(migrations);
        Validate();
    }

    /// <summary>
    /// Scans the Core assembly for every concrete <see cref="ISaveMigration"/>
    /// implementor with a public parameterless constructor.
    /// </summary>
    public static IEnumerable<ISaveMigration> DiscoverMigrations()
    {
        var assembly = typeof(SaveMigrationRegistry).Assembly;
        var marker = typeof(ISaveMigration);
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }
            if (!marker.IsAssignableFrom(type))
            {
                continue;
            }
            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"{type.FullName}: ISaveMigration must declare a public parameterless constructor for auto-discovery.");
            }
            yield return (ISaveMigration)ctor.Invoke(null);
        }
    }

    /// <summary>
    /// Orders the supplied migrations into a single contiguous chain by linking
    /// each <see cref="ISaveMigration.ToVersion"/> to the next migration's
    /// <see cref="ISaveMigration.FromVersion"/>.
    /// </summary>
    private static List<ISaveMigration> BuildChain(IEnumerable<ISaveMigration> migrations)
    {
        var all = migrations.ToList();
        if (all.Count == 0)
        {
            return all;
        }

        var byFrom = new Dictionary<string, ISaveMigration>(StringComparer.Ordinal);
        foreach (var m in all)
        {
            if (byFrom.ContainsKey(m.FromVersion))
            {
                throw new InvalidOperationException(
                    $"Duplicate save migration: two ISaveMigration implementors both declare FromVersion '{m.FromVersion}' " +
                    $"({byFrom[m.FromVersion].GetType().FullName} and {m.GetType().FullName}).");
            }
            byFrom[m.FromVersion] = m;
        }

        // A "head" is a migration whose FromVersion is not any other migration's ToVersion.
        var toVersions = new HashSet<string>(all.Select(m => m.ToVersion), StringComparer.Ordinal);
        var heads = all.Where(m => !toVersions.Contains(m.FromVersion)).ToList();
        if (heads.Count != 1)
        {
            throw new InvalidOperationException(
                $"Save migration chain must have exactly one head (a migration whose FromVersion is not produced by any other migration); found {heads.Count}. " +
                $"Heads: [{string.Join(", ", heads.Select(h => $"{h.GetType().Name}({h.FromVersion}→{h.ToVersion})"))}]");
        }

        var ordered = new List<ISaveMigration>(all.Count);
        var current = heads[0];
        while (current != null)
        {
            ordered.Add(current);
            if (byFrom.TryGetValue(current.ToVersion, out var next))
            {
                current = next;
            }
            else
            {
                current = null;
            }
        }

        if (ordered.Count != all.Count)
        {
            var missed = all.Except(ordered).Select(m => m.GetType().Name);
            throw new InvalidOperationException(
                $"Save migration chain is disconnected — {all.Count - ordered.Count} migration(s) not reachable from the head: [{string.Join(", ", missed)}].");
        }

        return ordered;
    }

    /// <summary>
    /// Applies all necessary migrations to bring <paramref name="root"/> from its
    /// current version up to <see cref="SaveData.CurrentVersion"/>.
    /// Returns the final version string after all migrations have been applied.
    /// </summary>
    /// <exception cref="SaveMigrationException">
    /// Thrown if the version is unrecognized or no migration path exists.
    /// </exception>
    public string Migrate(JsonObject root)
    {
        var version = root["Version"]?.GetValue<string>();
        if (string.IsNullOrEmpty(version))
        {
            version = "1.0.0";
            root["Version"] = version;
        }

        if (version == SaveData.CurrentVersion)
        {
            return version;
        }

        int appliedCount = 0;
        while (version != SaveData.CurrentVersion)
        {
            var migration = FindMigration(version!);
            if (migration == null)
            {
                throw new SaveMigrationException(
                    $"No migration found from version '{version}' toward '{SaveData.CurrentVersion}'. " +
                    $"Applied {appliedCount} migration(s) before failure.");
            }

            migration.Apply(root);
            version = migration.ToVersion;
            appliedCount++;

            // Safety: prevent infinite loops from misconfigured migrations.
            if (appliedCount > _migrations.Count)
            {
                throw new SaveMigrationException(
                    $"Migration loop detected after {appliedCount} steps at version '{version}'.");
            }
        }

        return version;
    }

    /// <summary>Returns the number of registered migrations.</summary>
    public int Count => _migrations.Count;

    private ISaveMigration? FindMigration(string fromVersion)
    {
        for (int i = 0; i < _migrations.Count; i++)
        {
            if (_migrations[i].FromVersion == fromVersion)
            {
                return _migrations[i];
            }
        }
        return null;
    }

    private void Validate()
    {
        for (int i = 1; i < _migrations.Count; i++)
        {
            if (_migrations[i].FromVersion != _migrations[i - 1].ToVersion)
            {
                throw new InvalidOperationException(
                    $"Save migration chain is broken: migration {i - 1} outputs '{_migrations[i - 1].ToVersion}' " +
                    $"but migration {i} expects '{_migrations[i].FromVersion}'.");
            }
        }
    }
}

/// <summary>
/// Thrown when save migration fails due to an unrecognized version or broken chain.
/// </summary>
public sealed class SaveMigrationException : Exception
{
    public SaveMigrationException(string message) : base(message) { }
}
