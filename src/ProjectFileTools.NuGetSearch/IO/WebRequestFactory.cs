using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectFileTools.NuGetSearch.IO
{
    public class WebRequestFactory : IWebRequestFactory
    {
        public async Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(endpoint).ConfigureAwait(false);
                return response;
            }
            catch
            {
                return null;
            }
        }
    }
}
