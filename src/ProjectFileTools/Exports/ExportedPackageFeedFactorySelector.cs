using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.Feeds;

namespace ProjectFileTools.Exports
{
    [Export(typeof(IPackageFeedFactorySelector))]
    [Name("Default Package Feed Factory Selector")]
    internal class ExportedPackageFeedFactorySelector : PackageFeedFactorySelector
    {
        [ImportingConstructor]
        public ExportedPackageFeedFactorySelector([ImportMany] IEnumerable<IPackageFeedFactory> feedFactories)
            : base(feedFactories)
        {
        }
    }
}