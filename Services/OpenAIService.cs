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
            this.apiKey = apiKey?.Trim() ?? string.Empty;
            this.model = string.IsNullOrWhiteSpace(model)
                ? "gpt-5-mini"
                : model.Trim();
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "OpenAI API key is missing.";

            if (string.IsNullOrWhiteSpace(prompt))
                return "Please enter a prompt.";

            try
            {
                var requestData = new
                {
                    model = model,
                    input = prompt
                };

                string json = JsonSerializer.Serialize(requestData);

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/responses");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                request.Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage response =
                    await http.SendAsync(request);

                string responseText =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseText);

                        if (errorDoc.RootElement.TryGetProperty(
                                "error", out JsonElement error) &&
                            error.TryGetProperty(
                                "message", out JsonElement message))
                        {
                            return $"OpenAI API error ({(int)response.StatusCode}): " +
                                   message.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                        // Use the HTTP status if the error body is not JSON.
                    }

                    return $"OpenAI API error: HTTP {(int)response.StatusCode} " +
                           $"{response.ReasonPhrase}";
                }

                using var document = JsonDocument.Parse(responseText);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("output_text", out JsonElement outputText) &&
                    outputText.ValueKind == JsonValueKind.String)
                {
                    string? result = outputText.GetString();

                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }

                // Fallback: extract text from output content blocks.
                if (root.TryGetProperty("output", out JsonElement output) &&
                    output.ValueKind == JsonValueKind.Array)
                {
                    var resultBuilder = new StringBuilder();

                    foreach (JsonElement item in output.EnumerateArray())
                    {
                        if (!item.TryGetProperty(
                                "content", out JsonElement content) ||
                            content.ValueKind != JsonValueKind.Array)
                            continue;

                        foreach (JsonElement contentItem in content.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty(
                                    "text", out JsonElement textElement) &&
                                textElement.ValueKind == JsonValueKind.String)
                            {
                                resultBuilder.AppendLine(textElement.GetString());
                            }
                        }
                    }

                    if (resultBuilder.Length > 0)
                        return resultBuilder.ToString().Trim();
                }

                return "OpenAI returned a response, but no text could be extracted.";
            }
            catch (HttpRequestException)
            {
                return "Could not connect to OpenAI. Check your internet connection.";
            }
            catch (TaskCanceledException)
            {
                return "The OpenAI request timed out or was cancelled.";
            }
            catch (JsonException)
            {
                return "Could not read the response from OpenAI.";
            }
            catch (Exception ex)
            {
                return $"Unexpected OpenAI error: {ex.Message}";
            }
        }
    }
}

