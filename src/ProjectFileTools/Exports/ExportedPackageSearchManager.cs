using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.Search;

namespace ProjectFileTools.Exports
{
    [Export(typeof(IPackageSearchManager))]
    [Name("Default Package Search Manager")]
    internal class ExportedPackageSearchManager : PackageSearchManager
    {
        [ImportingConstructor]
        public ExportedPackageSearchManager(IPackageFeedRegistryProvider feedRegistry, IPackageFeedFactorySelector factorySelector)
            : base(feedRegistry, factorySelector)
        {
        }
    }
}
