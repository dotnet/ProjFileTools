using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace PackageFeedManager
{

    [Export(typeof(IPackageFeedFactorySelector))]
    [Name("Default Package Feed Factory Selector")]
    internal class PackageFeedFactorySelector : IPackageFeedFactorySelector
    {
        [ImportingConstructor]
        public PackageFeedFactorySelector([ImportMany] IEnumerable<IPackageFeedFactory> feedFactories)
        {
            FeedFactories = feedFactories;
        }

        public IEnumerable<IPackageFeedFactory> FeedFactories { get; }

        public IPackageFeed GetFeed(string source)
        {
            foreach(IPackageFeedFactory feed in FeedFactories)
            {
                if (feed.TryHandle(source, out IPackageFeed instance))
                {
                    return instance;
                }
            }

            return null;
        }
    }
}
