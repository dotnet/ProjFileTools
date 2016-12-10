using System;
using System.Threading;
using System.Threading.Tasks;

namespace PackageFeedManager
{

    public interface IPackageSearchManager
    {
        IPackageFeedSearchJob<Tuple<string, SourceKind>> SearchPackageNames(string prefix, string tfm);

        IPackageFeedSearchJob<Tuple<string, SourceKind>> SearchPackageVersions(string packageName, string tfm);

        IPackageFeedSearchJob<IPackageInfo> SearchPackageInfo(string packageId, string version, string tfm);
    }
}
