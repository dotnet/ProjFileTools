using System.Threading;
using System.Threading.Tasks;

namespace PackageFeedManager
{
    public interface IWebRequestFactory
    {
        Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken);
    }
}
