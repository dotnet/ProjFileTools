using System.Collections.Generic;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageVersionSearchResult
    {
        bool Success { get; }

        IReadOnlyList<string> Versions { get; }

        FeedKind SourceKind { get; }
    }
}
