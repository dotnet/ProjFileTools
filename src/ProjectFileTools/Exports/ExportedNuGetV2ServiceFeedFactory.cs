using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.Feeds.Web;
using ProjectFileTools.NuGetSearch.IO;

namespace ProjectFileTools.Exports
{
    [Export(typeof(IPackageFeedFactory))]
    [Name("Default NuGet v2 Service Feed Factory")]
    internal class ExportedNuGetV2ServiceFeedFactory : NuGetV2ServiceFeedFactory
    {
        [ImportingConstructor]
        public ExportedNuGetV2ServiceFeedFactory(IWebRequestFactory webRequestFactory)
            : base(webRequestFactory)
        {
        }
    }
}
