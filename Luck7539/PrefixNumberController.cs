using DevExpress.Pdf.Native.BouncyCastle.Ocsp;
using LuckBurnTK.Utils;
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
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly HttpClient _httpClient;

        public PrefixNumberController(string apiKey)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Common.UrlBurnAPI),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        /// <summary>
        /// Lấy thông tin đầu số để thực hiện call hoặc sms
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<GetPrefixSmsRes> GetPrefixNumber(GetPrefixSmsReq req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("sms-call-center/get-prefix-number-7539", content);
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
                logger.Error($"GetPrefixNumber ERROR: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Gửi thông tin đã hoàn thành cuộc gọi lên tổng đài - để chờ tổng đài xác nhận
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<bool> ReleaseSlot(ReleaseSlotReq req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("sms-call-center/release-slot", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.Error($"ReleaseSlot ERROR: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gửi bản ghi từ thiết bị ghi âm được lên server
        /// </summary>
        /// <param name="history_id"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<bool> ReleaseUploadFile(string history_id, string filePath = null)
        {
            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent(history_id), "history_id");
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            var audioContent = new StreamContent(stream);
                            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/amr");
                            form.Add(audioContent, "audio_file", Path.GetFileName(filePath));
                            var request = new HttpRequestMessage(HttpMethod.Post, "sms-call-center/release-upload-file")
                            {
                                Content = form
                            };
                            var response = await _httpClient.SendAsync(request);
                            return response.IsSuccessStatusCode;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"ReleaseUploadFile ERROR: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Báo cáo thống kê tổng
        /// </summary>
        /// <param name="fromdate"></param>
        /// <param name="todate"></param>
        /// <returns></returns>
        public async Task<GetRevenueTotalRes[]> GetRevenueTotal(string fromdate, string todate)
        {
            try
            {
                var response = await _httpClient.GetAsync($"report/get-revenue-total?from_date={fromdate}&to_date={todate}");
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GetRevenueTotalRes[]>(responseBody);
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error($"GetRevenueTotal ERROR: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Báo cáo thống kê chi tiết
        /// </summary>
        /// <param name="fromdate"></param>
        /// <param name="todate"></param>
        /// <returns></returns>
        public async Task<GetRevenueDetailRes[]> GetRevenueDetail(string fromdate, string todate)
        {
            try
            {
                var response = await _httpClient.GetAsync($"report/get-revenue-detail?from_date={fromdate}&to_date={todate}");
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GetRevenueDetailRes[]>(responseBody);
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error($"GetRevenueDetail ERROR: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Lấy notification của hệ thống mỗi 15 phút/lần
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetNotification()
        {
            try
            {
                var response = await _httpClient.GetAsync($"config/notification");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error($"GetNotification ERROR: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Lấy thông tin về tin nhắn đã gửi đi từ VMG trả về
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<VMGSmsRes> GetVMGSms(VMGSmsRes req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "Http://103.68.240.22:8077/api_km.php");
                var content = new StringContent(jsonData, null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<VMGSmsRes>(responseBody);
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.Error($"GetVMGSms ERROR: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Cập nhật thông tin của VMG sau khi gửi tin nhắn
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<bool> UpdateSms(SmsReq req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("sms-call-center/sms", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateVMGsms ERROR: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy thông tin sim để chuyển tiền vào
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public async Task<TranferMoneyRes> GetSimTopupPool(TranferMoneyReq req)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("tranfer-money", content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TranferMoneyRes>(responseBody);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"GetSimTopupPool ERROR: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Update thông tin chuyển tiền
        /// </summary>
        /// <param name="history_id"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> UpdateStatusGetSimTopupPool(string history_id, string type)
        {
            try
            {
                var response = await _httpClient.GetAsync($"tranfer-money?history_id={history_id}&type={type}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.Error($"GetRevenueDetail ERROR: {ex.Message}");
                throw ex;
            }
        }
    }
}