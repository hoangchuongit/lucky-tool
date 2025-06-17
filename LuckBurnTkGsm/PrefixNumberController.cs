using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static LuckBurnTK.Models.PrefixNumberDto;

namespace LuckBurnTK
{
    public class PrefixNumberController
    {
        private readonly HttpClient _httpClient;

        public PrefixNumberController(string apiKey)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://luckburn.mobi/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        public async Task<GetPrefixSmsRes> GetPrefixNumber(GetPrefixSmsReq req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("sms-call-center/get-prefix-number", content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GetPrefixSmsRes>(responseBody);
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict) return null;
                throw new Exception("Không có dịch vụ tương ứng");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ReleaseSlot(ReleaseSlotReq req, string filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy file", filePath);
            try
            {
                using (var form = new MultipartFormDataContent())
                using (var stream = File.OpenRead(filePath))
                using (var audioContent = new StreamContent(stream))
                using (var request = new HttpRequestMessage(HttpMethod.Post, "sms-call-center/release-slot"))
                {
                    audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/amr");
                    form.Add(new StringContent(req.request_id), "request_id");
                    form.Add(new StringContent(req.history_id), "history_id");
                    form.Add(new StringContent(req.prefix), "prefix");
                    form.Add(new StringContent(req.prefix_unit), "prefix_unit");
                    form.Add(new StringContent(req.start_call), "start_call");
                    form.Add(new StringContent(req.end_call), "end_call");
                    form.Add(new StringContent(req.duration.ToString()), "duration");
                    form.Add(new StringContent(req.no_carrier.ToString()), "no_carrier");
                    if (!string.IsNullOrEmpty(filePath)) form.Add(audioContent, "audio_file", Path.GetFileName(filePath));
                    request.Content = form;
                    var response = await _httpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
