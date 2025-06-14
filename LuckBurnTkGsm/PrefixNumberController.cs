using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static LuckBurnTK.Models.PrefixNumberDto;

namespace LuckBurnTK
{
    public class PrefixNumberController
    {
        private readonly HttpClient _httpClient;

        public PrefixNumberController()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://luckburn.mobi/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<GetPrefixSmsRes> GetPrefixNumber(GetPrefixSmsReq req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync("sms-call-center/get-prefix-number", content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    GetPrefixSmsRes result = JsonConvert.DeserializeObject<GetPrefixSmsRes>(responseBody);
                    return result;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
