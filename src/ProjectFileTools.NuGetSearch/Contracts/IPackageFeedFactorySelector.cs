using System.Collections.Generic;

namespace ProjectFileTools.NuGetSearch.Contracts
{
    public interface IPackageFeedFactorySelector
    {
        IEnumerable<IPackageFeedFactory> FeedFactories { get; }

        IPackageFeed GetFeed(string source);
    }
}
