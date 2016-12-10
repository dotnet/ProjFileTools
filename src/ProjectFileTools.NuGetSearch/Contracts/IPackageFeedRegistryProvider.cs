using System.Collections.Generic;

namespace PackageFeedManager
{

    public interface IPackageFeedRegistryProvider
    {
        IReadOnlyList<string> ConfiguredFeeds { get; }
    }
}
