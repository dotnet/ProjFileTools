using System.Collections.Generic;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageFeedRegistryProvider
    {
        IReadOnlyList<string> ConfiguredFeeds { get; }
    }
}
