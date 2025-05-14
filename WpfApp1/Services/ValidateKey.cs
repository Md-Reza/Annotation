using System.Net.Http;
using System.Threading.Tasks;
using WpfApp1.Base;
using WpfApp1.Model;

namespace WpfApp1.Services
{
    public sealed class ValidateKey
    {
        public static async Task<HttpResponseMessage> GenerateKeyAsync(ValidateViewModal licenceViewModal)
        {
            var res = await Client.PostAsync($"license/validate", licenceViewModal).ConfigureAwait(false);
            return res;
        }
    }
}
