using System.Collections.Generic;

namespace PackageFeedManager
{

    public interface IPackageNameSearchResult
    {
        bool Success { get; }

        FeedKind SourceKind { get; }

        IReadOnlyList<string> Names { get; }
    }
}
