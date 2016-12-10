using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace PackageFeedManager
{

    [Export(typeof(IWebRequestFactory))]
    [Name("Default Web Request Factory")]
    internal class WebRequestFactory : IWebRequestFactory
    {
        public async Task<string> GetStringAsync(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                WebResponse response = await WebRequest.CreateHttp(endpoint).GetResponseAsync().ConfigureAwait(false);
                using (Stream s = response.GetResponseStream())
                using (StreamReader r = new StreamReader(s, Encoding.UTF8, true, 8192, true))
                {
                    return await r.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
