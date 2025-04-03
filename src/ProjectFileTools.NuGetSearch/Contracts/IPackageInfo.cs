using System.Collections.Generic;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.NuGetSearch.Contracts
{
    public interface IPackageInfo
    {
        string Id { get; }

        string Version { get; }

        string Title { get; }

        string Authors { get; }

        string Summary { get; }

        string Description { get; }

        string LicenseUrl { get; }

        string ProjectUrl { get; }

        string IconUrl { get; }

        string Tags { get; }

        IReadOnlyList<PackageType> PackageTypes { get; }

        FeedKind SourceKind { get; }
    }
}
