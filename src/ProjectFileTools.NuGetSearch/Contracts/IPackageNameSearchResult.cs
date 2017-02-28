using System.Collections.Generic;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageNameSearchResult
    {
        bool Success { get; }

        FeedKind SourceKind { get; }

        IReadOnlyList<string> Names { get; }
    }
}
