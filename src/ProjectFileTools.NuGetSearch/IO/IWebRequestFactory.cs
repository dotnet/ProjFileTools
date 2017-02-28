using System.Threading;
using System.Threading.Tasks;

namespace ProjectFileTools.NuGetSearch.IO
{
    public interface IWebRequestFactory
    {
        Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken);
    }
}
