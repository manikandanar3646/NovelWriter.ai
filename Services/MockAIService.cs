
using System;
using System.Threading.Tasks;

namespace AI_Novel_writing_System.Services
{
    /// <summary>
    /// Simulated AI provider for development and testing.
    /// Does not require an API key, internet access, or a local model.
    /// </summary>
    public sealed class MockAIService : IAIService
    {
        public Task<string> GenerateResponseAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Task.FromResult(
                    "Please enter a prompt so I can assist with your story.");
            }

            string response =
                "[DEVELOPMENT MODE — SIMULATED AI RESPONSE]\n\n" +
                "Your prompt has been received successfully.\n\n" +
                "Prompt summary: " + prompt.Trim() + "\n\n" +
                "This is a placeholder response used to test the " +
                "MythForge writing workflow. No real AI model was called.\n\n" +
                "When a real AI provider is configured, this response " +
                "will be replaced with generated story content.";

            return Task.FromResult(response);
        }
    }
}
