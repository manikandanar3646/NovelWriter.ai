using AI_Novel_writing_System;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace darkFanNovel
{
    public class OpenAIService : IAIService
    {
        private static readonly HttpClient http = new HttpClient();

        private readonly string apiKey;
        private readonly string model;

        public OpenAIService(
            string apiKey,
            string model = "gpt-5-mini")
        {
            this.apiKey = apiKey;
            this.model = model;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return "❌ OpenAI API key is missing.";
                }

                var requestData = new
                {
                    model = model,
                    input = prompt
                };

                string json =
                    JsonSerializer.Serialize(requestData);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses"
                );

                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        apiKey
                    );

                request.Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                var response =
                    await http.SendAsync(request);

                string responseText =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ OpenAI Error:\n{responseText}";
                }

                using var doc =
                    JsonDocument.Parse(responseText);

                if (doc.RootElement.TryGetProperty(
                    "output_text",
                    out JsonElement outputText))
                {
                    return outputText.GetString()
                           ?? "⚠️ Empty response from OpenAI.";
                }

                /*
                 * Fallback parser in case output_text
                 * isn't returned in the expected form.
                 */

                if (doc.RootElement.TryGetProperty(
                    "output",
                    out JsonElement output))
                {
                    foreach (var item in output.EnumerateArray())
                    {
                        if (item.TryGetProperty(
                            "content",
                            out JsonElement content))
                        {
                            foreach (var contentItem
                                     in content.EnumerateArray())
                            {
                                if (contentItem.TryGetProperty(
                                    "text",
                                    out JsonElement text))
                                {
                                    return text.GetString()
                                           ?? "⚠️ Empty response.";
                                }
                            }
                        }
                    }
                }

                return "⚠️ OpenAI returned an unexpected response.";
            }
            catch (HttpRequestException)
            {
                return "❌ Could not connect to OpenAI.\n\n" +
                       "Check your internet connection.";
            }
            catch (Exception ex)
            {
                return $"❌ OpenAI Error: {ex.Message}";
            }
        }
    }
}