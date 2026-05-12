using System.Reflection;

namespace LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;

public static class AttributeTableStore
{
    static readonly object s_gate = new();
    static readonly List<AttributeTable> s_tables = new();
    static readonly HashSet<Assembly> s_scannedAssemblies = new();
    static bool s_designToolsAssembliesLoaded;

    public static void RegisterAttributeTable(AttributeTable table)
    {
        lock (s_gate)
            s_tables.Add(table);
    }

    public static void RegisterMetadataProvider(IProvideAttributeTable provider)
    {
        RegisterAttributeTable(provider.AttributeTable);
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        lock (s_gate)
        {
            if (!s_scannedAssemblies.Add(assembly))
                return;
        }

        foreach (var attribute in assembly.GetCustomAttributes<ProvideMetadataAttribute>())
        {
            if (Activator.CreateInstance(attribute.MetadataProviderType, nonPublic: true) is IProvideAttributeTable provider)
                RegisterMetadataProvider(provider);
        }
    }

    public static IEnumerable<Attribute> GetCustomAttributes(Type componentType, string propertyName)
    {
        EnsureLoadedAssembliesScanned();

        AttributeTable[] tables;
        lock (s_gate)
            tables = s_tables.ToArray();

        foreach (var table in tables)
        {
            foreach (var attribute in table.GetCustomAttributes(componentType, propertyName))
                yield return attribute;
        }
    }

    static void EnsureLoadedAssembliesScanned()
    {
        EnsureDesignToolsAssembliesLoaded();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            RegisterAssembly(assembly);
    }

    static void EnsureDesignToolsAssembliesLoaded()
    {
        lock (s_gate)
        {
            if (s_designToolsAssembliesLoaded)
                return;

            s_designToolsAssembliesLoaded = true;
        }

        foreach (var folder in GetDesignToolsSearchFolders())
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (var path in Directory.EnumerateFiles(folder, "*.DesignTools.dll", SearchOption.TopDirectoryOnly))
                TryLoadAssembly(path);
        }
    }

    static IEnumerable<string> GetDesignToolsSearchFolders()
    {
        if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
            yield return AppContext.BaseDirectory;

        var currentDirectory = Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory)
            && !string.Equals(currentDirectory, AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
            yield return currentDirectory;
    }

    static void TryLoadAssembly(string path)
    {
        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName)))
                return;

            RegisterAssembly(Assembly.LoadFrom(path));
        }
        catch
        {
            // Some targets do not expose ordinary design-tools DLLs or allow dynamic loading.
            // Loaded assemblies are still scanned, so metadata remains available when the host
            // explicitly references or preloads a design-tools assembly.
        }
    }
}
