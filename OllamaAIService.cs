using AI_Novel_writing_System;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace darkFanNovel
{
    public class OllamaAIService : IAIService
    {
        private static readonly HttpClient http = new HttpClient();

        private readonly string model;

        public OllamaAIService(string model = "llama3.1")
        {
            this.model = model;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            try
            {
                var requestData = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false
                };

                string json = JsonSerializer.Serialize(requestData);

                var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await http.PostAsync(
                    "http://localhost:11434/api/generate",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Ollama Error: {response.StatusCode}";
                }

                string responseText =
                    await response.Content.ReadAsStringAsync();

                using var doc =
                    JsonDocument.Parse(responseText);

                if (doc.RootElement.TryGetProperty(
                    "response",
                    out JsonElement resp))
                {
                    return resp.GetString()
                           ?? "⚠️ Empty response from Llama.";
                }

                return "⚠️ Ollama did not return a valid response.";
            }
            catch (HttpRequestException)
            {
                return "❌ Cannot connect to Ollama.\n\n" +
                       "Make sure Ollama is running and the Llama model is installed.";
            }
            catch (Exception ex)
            {
                return $"❌ Ollama Error: {ex.Message}";
            }
        }
    }
}