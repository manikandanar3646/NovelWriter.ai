using AI_Novel_writing_System.Data;
using AI_Novel_writing_System.Models;
using AI_Novel_writing_System;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace darkFanNovel
{
    public partial class MainWindow : Window
    {
        private string currentMode = "creative";
        private IAIService? aiService;

        private readonly DatabaseService databaseService;
        private readonly NovelRepository novelRepository;
        private readonly ChapterRepository chapterRepository;
        private readonly CharacterRepository characterRepository;

        public MainWindow()
        {
            InitializeComponent();

            // ============================
            // AI
            // ============================

            aiService = new OllamaAIService();

            // ============================
            // DATABASE
            // ============================

            databaseService = new DatabaseService();

            novelRepository =
                new NovelRepository(databaseService);

            chapterRepository =
                new ChapterRepository(databaseService);

            characterRepository =
                new CharacterRepository(databaseService);
        }

        // ============================
        // CREATIVE MODE
        // ============================

        private void CreativeModeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            currentMode = "creative";

            AIResponseBox.Text =
                "✨ Creative Mode Activated.\n" +
                "AI will respond with imagination and fantasy.";
        }

        // ============================
        // MANUAL MODE
        // ============================

        private void ManualModeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            currentMode = "manual";

            AIResponseBox.Text =
                "✒️ Manual Mode Activated.\n" +
                "AI will correct grammar only.";
        }

        // ============================
        // PROVIDER SELECTION
        // ============================

        private void AIProviderComboBox_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (AIProviderComboBox.SelectedIndex == 0)
            {
                // Local Llama
                aiService = new OllamaAIService();

                APIKeyBox.Visibility =
                    Visibility.Collapsed;

                APIKeyLabel.Visibility =
                    Visibility.Collapsed;

                ModelBox.Visibility =
                    Visibility.Collapsed;

                ModelLabel.Visibility =
                    Visibility.Collapsed;

                AIResponseBox.Text =
                    "🖥️ Local AI selected.\n\n" +
                    "Using Ollama + Llama 3.1.\n" +
                    "No API key required.";
            }
            else
            {
                // OpenAI
                APIKeyBox.Visibility =
                    Visibility.Visible;

                APIKeyLabel.Visibility =
                    Visibility.Visible;

                ModelBox.Visibility =
                    Visibility.Visible;

                ModelLabel.Visibility =
                    Visibility.Visible;

                AIResponseBox.Text =
                    "☁️ OpenAI Cloud selected.\n\n" +
                    "Enter your OpenAI API key.";
            }
        }

        // ============================
        // SEND MESSAGE
        // ============================

        private async void SendMessageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string userMessage =
                UserInputBox.Text;

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                AIResponseBox.Text =
                    "❗ Please type a message first.";

                return;
            }

            // ============================
            // OPENAI CONFIGURATION
            // ============================

            if (AIProviderComboBox.SelectedIndex == 1)
            {
                string apiKey = APIKeyBox.Password.Trim();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    AIResponseBox.Text =
                        "❌ Please enter your OpenAI API key.";

                    return;
                }

                string model =
                    string.IsNullOrWhiteSpace(ModelBox.Text)
                        ? "gpt-5-mini"
                        : ModelBox.Text.Trim();

                aiService =
                    new OpenAIService(
                        apiKey,
                        model
                    );
            }

            // ============================
            // THINKING
            // ============================

            AIResponseBox.Text =
                "⏳ Thinking...";

            // ============================
            // CREATE PROMPT
            // ============================

            string prompt;

            if (currentMode == "creative")
            {
                prompt =
                    $"You are an AI novel writing assistant " +
                    $"specialized in dark fantasy fiction.\n\n" +
                    $"Write a creative and immersive response " +
                    $"to the following request:\n\n" +
                    $"{userMessage}";
            }
            else
            {
                prompt =
                    $"You are a professional writing editor.\n\n" +
                    $"Correct the grammar, spelling, punctuation, " +
                    $"and sentence structure of the following text.\n\n" +
                    $"Do not change the original meaning:\n\n" +
                    $"{userMessage}";
            }

            // ============================
            // GENERATE RESPONSE
            // ============================

            if (aiService == null)
            {
                AIResponseBox.Text =
                    "❌ AI provider is not configured.";

                return;
            }

            string result =
                await aiService.GenerateResponseAsync(prompt);

            AIResponseBox.Text = result;
        }
    }
}