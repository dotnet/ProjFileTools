using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectFileTools.NuGetSearch.IO;

namespace PackageFeedManagerTests
{
    public class MockWebRequestFactory : IWebRequestFactory
    {
        public Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
