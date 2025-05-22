using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace TranscriptionService
{
    /// <summary>
    /// Client for interacting with the Whisper Transcription API
    /// </summary>
    public class LuckVoiceToText : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl;
        private readonly Logger _logger;
        private readonly bool _disposeClient;
        private bool _disposed = false;

        private readonly int _maxWaitTimeSeconds = 300;
        private readonly int _pollingIntervalMs = 2000;

        /// <summary>
        /// Creates a new instance of the LuckVoiceToText
        /// </summary>
        /// <param name="apiBaseUrl">Base URL of the transcription API</param>
        /// <param name="apiKey">API key for authentication</param>
        /// <param name="logger">NLog Logger instance</param>
        /// <param name="client">Optional existing HttpClient instance</param>
        public LuckVoiceToText(string apiBaseUrl, string apiKey, Logger logger, HttpClient client = null)
        {
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _logger = logger ?? LogManager.GetCurrentClassLogger();

            if (client != null)
            {
                _client = client;
                _disposeClient = false;
            }
            else
            {
                _client = new HttpClient();
                _disposeClient = true;
            }

            // Set the API key header
            _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        /// <summary>
        /// Transcribes an audio file to text
        /// </summary>
        /// <param name="filePath">Path to the audio file</param>
        /// <returns>Transcribed text result</returns>
        public async Task<string> TranscribeAudioAsync(string filePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LuckVoiceToText));

            try
            {
                // Step 1: Submit the audio file for transcription
                string taskId = await SubmitTranscriptionTaskAsync(filePath);
                // Step 2: Poll until the result is ready
                string result = await PollForTranscriptionResultAsync(taskId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in transcription process: {ex.Message}");
                throw;
            }
        }

        private async Task<string> SubmitTranscriptionTaskAsync(string filePath)
        {
            // Create a unique filename based on source and timestamp
            string fileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(filePath)}";

            // Use using statements to ensure proper disposal
            using (var form = new MultipartFormDataContent())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var fileContent = new StreamContent(fileStream))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                form.Add(fileContent, "file", fileName);

                // Send the request to the API
                using (HttpResponseMessage response = await _client.PostAsync($"{_apiBaseUrl}/transcribe/", form))
                {
                    // Ensure we got a success response
                    response.EnsureSuccessStatusCode();

                    // Parse the response to get the task ID
                    string responseBody = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        return doc.RootElement.GetProperty("task_id").GetString();
                    }
                }
            }
        }

        private async Task<string> PollForTranscriptionResultAsync(string taskId)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan timeout = TimeSpan.FromSeconds(_maxWaitTimeSeconds);

            while (DateTime.Now - startTime < timeout)
            {
                // Get the current status of the task
                using (HttpResponseMessage response = await _client.GetAsync($"{_apiBaseUrl}/transcribe/{taskId}"))
                {
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        string status = doc.RootElement.GetProperty("status").GetString();

                        if (status == "completed")
                        {
                            // Task is complete, return the processed text
                            return doc.RootElement.GetProperty("processed_text").GetString();
                        }
                        else if (status == "failed")
                        {
                            // Task failed, throw an exception with the error message
                            string errorMessage = doc.RootElement.GetProperty("error").GetString();
                            throw new Exception($"Transcription failed: {errorMessage}");
                        }
                        else
                        {
                            // Task is still pending, wait for a bit then check again
                            int queuePosition = doc.RootElement.GetProperty("queue_position").GetInt32();
                            _logger.Debug($"Task {taskId} still pending. Queue position: {queuePosition}");
                            await Task.Delay(_pollingIntervalMs);
                        }
                    }
                }
            }

            // If we've reached here, the task has timed out
            throw new TimeoutException($"Transcription task {taskId} did not complete within the allowed time of {_maxWaitTimeSeconds} seconds.");
        }

        /// <summary>
        /// Disposes the HttpClient if it was created by this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && _disposeClient)
            {
                _client?.Dispose();
            }

            _disposed = true;
        }
    }
}