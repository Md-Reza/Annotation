using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Base
{
    public static class Client
    {
        private static HttpClient httpClient;
        static string secKey = "SMG4144DkYoaQeSw6dyGt0w8q3e2xOI7eWb6ChomhcTikoMOZY=";
        static string baseUrl = "https://eteach-dev.vercel.app/api/";
        static string token = "";

        public static async Task<HttpResponseMessage> PostAnonymousAsync<T>(string baseUrl, string url, T data)
        {

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
            };
            httpClient.DefaultRequestHeaders.Add("api_secret_key", secKey);

            var stringContent = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, stringContent);
            return response;
        }
        public static async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        {
            var handler = SHttpRequestHandler(token);
            var stringContent = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await handler.PostAsync(url, stringContent);

            return response;
        }


        private static HttpClient SHttpRequestHandler(string token = null)
        {
            

            httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                DefaultRequestHeaders = {
                    Accept = { new MediaTypeWithQualityHeaderValue("application/json"), },
                  //  Authorization = new AuthenticationHeaderValue("Bearer", token)
                }
            };

            httpClient.DefaultRequestHeaders.Add("api_secret_key", secKey);

            return httpClient;
        }
    }
}
