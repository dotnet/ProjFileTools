using System;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageSearchManager
    {
        IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageNames(string prefix, string tfm);

        IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageVersions(string packageName, string tfm);

        IPackageFeedSearchJob<IPackageInfo> SearchPackageInfo(string packageId, string version, string tfm);
    }
}
