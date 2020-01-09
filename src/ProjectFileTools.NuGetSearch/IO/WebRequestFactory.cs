using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectFileTools.NuGetSearch.IO
{
    public class WebRequestFactory : IWebRequestFactory
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public async Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage responseMessage = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // avoid a first-chance exception on 404
                    return null;
                }

                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }
    }
}
