using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace WpfApp1.Base
{
    public static class SDeserializer
    {
        public static T Deserialize<T>(HttpResponseMessage httpResponse)
        {
            return JsonConvert.DeserializeObject<T>(httpResponse.Content.ReadAsStringAsync().Result);
        }
        public static IEnumerable<T> DeserializeAsEnum<T>(HttpResponseMessage httpResponse)
        {
            return JsonConvert.DeserializeObject<IEnumerable<T>>(httpResponse.Content.ReadAsStringAsync().Result);
        }
        public static List<T> DeserializeAsList<T>(HttpResponseMessage httpResponse)
        {
            return JsonConvert.DeserializeObject<List<T>>(httpResponse.Content.ReadAsStringAsync().Result).ToList();
        }
        public static string DeserializeError(HttpResponseMessage httpResponse)
        {
            return httpResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError
                ? $"Oops! Internal Server Error. \nRequested Url: {httpResponse.RequestMessage.RequestUri}"
                : httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? $"Oops! Unauthorized Error. The client could not be authenticated. \nRequested Url: {httpResponse.RequestMessage.RequestUri}"
                : httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound
                ? $"Oops! The requested resource is not found. \nRequested Url: {httpResponse.RequestMessage.RequestUri}"
                : httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed
                ? $"Oops! The requested resource is not allowed to perform operations. \nRequested Url: {httpResponse.RequestMessage.RequestUri}"
                : JsonConvert.DeserializeObject<string>(httpResponse.Content.ReadAsStringAsync().Result);
        }
    }
}
