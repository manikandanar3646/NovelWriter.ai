
using AI_Novel_writing_System;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AI_Novel_writing_System.Services
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
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return "Please enter a prompt for your novel.";
                }

                var requestData = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false
                };

                string json = JsonSerializer.Serialize(requestData);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                using var response = await http.PostAsync(
                    "http://localhost:11434/api/generate",
                    content
                );

                string responseText =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Ollama error: {(int)response.StatusCode} " +
                           $"{response.ReasonPhrase}\n{responseText}";
                }

                using var doc = JsonDocument.Parse(responseText);

                if (doc.RootElement.TryGetProperty(
                    "response",
                    out JsonElement resp))
                {
                    return resp.GetString()
                           ?? "Empty response from the local model.";
                }

                return "Ollama did not return a valid response.";
            }
            catch (HttpRequestException)
            {
                return "Cannot connect to Ollama.\n\n" +
                       "Make sure Ollama is running and the llama3.1 model is installed.";
            }
            catch (TaskCanceledException)
            {
                return "The Ollama request timed out. " +
                       "The model may be taking too long to respond.";
            }
            catch (Exception ex)
            {
                return $"Ollama error: {ex.Message}";
            }
        }
    }
}
