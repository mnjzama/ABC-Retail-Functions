using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace RetailAppMVC.Helpers
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

        public static void SetJson<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value, _options));
        }

        public static T? GetJson<T>(this ISession session, string key)
        {
            var jsonData = session.GetString(key);
            return jsonData == null ? default : JsonSerializer.Deserialize<T>(jsonData, _options);
        }
    }
}