using System.Collections.Generic;

namespace PackageFeedManager
{
    public interface IPackageFeedFactorySelector
    {
        IEnumerable<IPackageFeedFactory> FeedFactories { get; }

        IPackageFeed GetFeed(string source);
    }
}
