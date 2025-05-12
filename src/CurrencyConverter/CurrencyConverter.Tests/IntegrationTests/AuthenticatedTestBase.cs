using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Tests.IntegrationTests
{
    public abstract class AuthenticatedTestBase
    {
        protected readonly HttpClient _client;

        protected AuthenticatedTestBase(HttpClient client)
        {
            _client = client;
        }

        protected async Task<string> GetJwtTokenAsync(string username = "admin@example.com", string password = "Admin123!")
        {
            var response = await _client.PostAsJsonAsync("/api/Auth/login", new { UserName = username, Password = password });
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return content["token"];
        }
    }

}
