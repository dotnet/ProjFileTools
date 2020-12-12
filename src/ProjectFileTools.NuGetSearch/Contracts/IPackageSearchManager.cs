using System;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.NuGetSearch.Contracts
{

    public interface IPackageSearchManager
    {
        IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageNames(string prefix, string tfm, string packageType = null);

        IPackageFeedSearchJob<Tuple<string, FeedKind>> SearchPackageVersions(string packageName, string tfm, string packageType = null);

        IPackageFeedSearchJob<IPackageInfo> SearchPackageInfo(string packageId, string version, string tfm);
    }
}
