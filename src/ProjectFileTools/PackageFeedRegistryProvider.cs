using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Utilities;
using ProjectFileTools.NuGetSearch.Contracts;
using ProjectFileTools.NuGetSearch.IO;

namespace ProjectFileTools
{
    [Export(typeof(IPackageFeedRegistryProvider))]
    [Name("Default Package Feed Registry Provider")]
    internal class PackageFeedRegistryProvider : IPackageFeedRegistryProvider
    {
        private readonly object _provider;
        private readonly IWebRequestFactory _webRequestFactory;

        [ImportingConstructor]
        public PackageFeedRegistryProvider([Import("NuGet.VisualStudio.IVsPackageSourceProvider")]object provider, IWebRequestFactory webRequestFactory)
        {
            _webRequestFactory = webRequestFactory;
            _provider = provider;
        }

        public IReadOnlyList<string> ConfiguredFeeds
        {
            get
            {
                List<string> sources = new List<string>();
                IEnumerable<KeyValuePair<string, string>> enabledSources = (IEnumerable<KeyValuePair<string, string>>)_provider.GetType().GetMethod("GetSources").Invoke(_provider, new object[] { true, false });

                foreach (KeyValuePair<string, string> curEnabledSource in enabledSources)
                {
                    string source = curEnabledSource.Value;
                    sources.Add(source);
                }

                if(!sources.Any(x => x.IndexOf("\\.nuget", StringComparison.OrdinalIgnoreCase) > -1))
                {
                    sources.Add(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.nuget\\packages"));
                }

                return sources;
            }
        }
    }
}
