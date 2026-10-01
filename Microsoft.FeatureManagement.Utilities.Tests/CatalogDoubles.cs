using System.Diagnostics.CodeAnalysis;

namespace Microsoft.FeatureManagement.Utilities.Tests;

public static class CatalogDoubles
{
    public const string UseCache = "Catalog.UseCache";

    public interface ICatalog
    {
        string Describe<TItem>();
    }

    public sealed class DefaultCatalog : ICatalog
    {
        public string Describe<TItem>()
            => $"default:{typeof(TItem).Name}";
    }

    public sealed class CachedCatalog
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A replacement is bound as an instance method group.")]
        public string Describe<TItem>()
            => $"cached:{typeof(TItem).Name}";
    }

    public sealed class StructCatalog
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A replacement is bound as an instance method group.")]
        public string Describe<TItem>()
            where TItem : struct
            => $"struct:{typeof(TItem).Name}";
    }

    public sealed class NonGenericCatalog
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "A replacement is bound as an instance method group.")]
        public string Describe()
            => "non-generic";
    }
}
