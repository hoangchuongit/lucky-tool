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

        public PrefixNumberController()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://luckburn.mobi/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<GetPrefixSmsRes> GetPrefixNumber(GetPrefixSmsReq req, string apikey)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                content.Headers.Add("x-api-key", apikey);
                HttpResponseMessage response = await _httpClient.PostAsync("sms-call-center/get-prefix-number", content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    GetPrefixSmsRes result = JsonConvert.DeserializeObject<GetPrefixSmsRes>(responseBody);
                    return result;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict) return null;
                throw new Exception("Không có dịch vụ tương ứng");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ReleaseSlot(ReleaseSlotReq req, string apikey)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                content.Headers.Add("x-api-key", apikey);
                HttpResponseMessage response = await _httpClient.PostAsync("sms-call-center/release-slot", content);
                if (response.IsSuccessStatusCode) return true;
                else return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ReleaseSlot(ReleaseSlotReq req, string apikey, string filePath)
        {
            //var client = new HttpClient();
            //var request = new HttpRequestMessage(HttpMethod.Post, "https://luckburn.mobi/api/sms-call-center/release-slot");
            //request.Headers.Add("x-api-key", "a7d52e81e3dbaa4be6a0d932c71cdc992099b98ff9d76789v4fcf554844c6c81");
            //var content = new MultipartFormDataContent();
            //content.Add(new StringContent("4f083aac-bbb5-4fb2-b65e-9b743c73429c"), "request_id");
            //content.Add(new StringContent("d2468e96-562a-46f0-ba1b-fd8e49d5da5b"), "history_id");
            //content.Add(new StringContent("1900077798"), "prefix");
            //content.Add(new StringContent("GTEL_1900"), "prefix_unit");
            //content.Add(new StringContent("21000"), "duration");
            //content.Add(new StreamContent(File.OpenRead("/D:/Freelancer/LuckTools/LuckBurnTkGsm/bin/Debug/record/0854793908_1900088865_20250616221913.amr")), "audio_file", "/D:/Freelancer/LuckTools/LuckBurnTkGsm/bin/Debug/record/0854793908_1900088865_20250616221913.amr");
            //request.Content = content;
            //var response = await client.SendAsync(request);
            //response.EnsureSuccessStatusCode();
            //Console.WriteLine(await response.Content.ReadAsStringAsync());

            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    if (!File.Exists(filePath)) throw new FileNotFoundException("Không tìm thấy file", filePath);
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/amr");
                        form.Add(fileContent, "audio_file", Path.GetFileName(filePath));
                        var json = JsonConvert.SerializeObject(req);
                        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                        form.Add(jsonContent, "body");
                        var request = new HttpRequestMessage(HttpMethod.Post, "sms-call-center/release-slot")
                        {
                            Content = form
                        };
                        request.Headers.Add("x-api-key", apikey);
                        using (var response = await _httpClient.SendAsync(request))
                        {
                            return response.IsSuccessStatusCode;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi gọi API release-slot: {ex.Message}");
                throw;
            }
        }

    }
}
