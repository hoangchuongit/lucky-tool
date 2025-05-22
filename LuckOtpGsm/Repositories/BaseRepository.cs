using LuckOTP.Model;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LuckOTP.Repositories
{
    public class BaseRepository
    {
        public static readonly HttpClient client = new HttpClient();

        public async Task<Guid> Login(string username, string password)
        {
            try
            {
                var loginData = new { email = username, password = password };
                var content = new StringContent(JsonConvert.SerializeObject(loginData), null, "application/json");

                var response = await client.PostAsync("https://emailreal.com/auth/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonConvert.DeserializeObject<Account>(result);

                    if (loginResponse != null)
                    {
                        // Gắn API_KEY vào header sau khi đăng nhập
                        client.DefaultRequestHeaders.Add("x-api-key", loginResponse.api_key);
                        return loginResponse.id;
                    }
                }
                throw new Exception("Login failed. Please check your credentials.");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }
    }
}