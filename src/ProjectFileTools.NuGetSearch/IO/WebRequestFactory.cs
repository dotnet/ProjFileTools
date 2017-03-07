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
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage responseMessage = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    responseMessage.EnsureSuccessStatusCode();
                    return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
