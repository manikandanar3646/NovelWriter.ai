using AI_Novel_writing_System.Data;
using AI_Novel_writing_System.Models;
using AI_Novel_writing_System.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AI_Novel_writing_System;

public partial class MainWindow : Window
{
    private readonly DatabaseService _database = new();
    private NovelRepository _novels = null!;
    private ChapterRepository _chapters = null!;
    private CharacterRepository _characters = null!;
    private StoryNoteRepository _notes = null!;

    private Novel? _currentNovel;
    private Chapter? _currentChapter;
    private IAIService _ai = new OllamaAIService();

    private bool _loading;
    private bool _isOpenAI;
    private string _apiKey = "";
    private string _mode = "creative";
    private string _lastSavedText = "";

    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    public MainWindow()
    {
        InitializeComponent();

        _novels = new NovelRepository(_database);
        _chapters = new ChapterRepository(_database);
        _characters = new CharacterRepository(_database);
        _notes = new StoryNoteRepository(_database);

        LoadNovels();
        UpdateWordCount();
    }

    private void LoadNovels(int selectId = 0)
    {
        _loading = true;

        var novels = _novels.GetAll();
        NovelSelector.ItemsSource = novels;
        NovelSelector.SelectedItem = novels.FirstOrDefault(
            n => n.Id == selectId) ?? novels.FirstOrDefault();

        _loading = false;

        if (NovelSelector.SelectedItem is Novel novel)
            LoadNovel(novel);
        else
        {
            _currentNovel = null;
            ChapterList.ItemsSource = null;
            ManuscriptEditor.Clear();
            CurrentChapterTitle.Text = "Create a novel to begin";
            ChapterSubheading.Text = "No novel selected";
        }
    }

    private void LoadNovel(Novel novel)
    {
        _currentNovel = novel;

        _loading = true;
        ChapterList.ItemsSource = _chapters.GetByNovel(novel.Id);
        ChapterList.SelectedIndex = ChapterList.Items.Count > 0 ? 0 : -1;
        _loading = false;

        ChapterSubheading.Text = $"{ChapterList.Items.Count} chapters";

        if (ChapterList.SelectedItem is Chapter chapter)
            LoadChapter(chapter);
        else
        {
            _currentChapter = null;
            ManuscriptEditor.Clear();
            CurrentChapterTitle.Text = novel.Title;
            _lastSavedText = "";
            UpdateWordCount();
        }
    }

    private void LoadChapter(Chapter chapter)
    {
        _currentChapter = chapter;

        _loading = true;
        ManuscriptEditor.Text = chapter.Content;
        CurrentChapterTitle.Text =
            $"Chapter {chapter.ChapterNumber} — {chapter.Title}";
        _lastSavedText = chapter.Content;
        _undo.Clear();
        _redo.Clear();
        _loading = false;

        SaveStatusLabel.Text = "Saved";
        AutosaveLabel.Text = "♧  Loaded from LiteDB";
        UpdateWordCount();
    }

    private void NovelSelector_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && NovelSelector.SelectedItem is Novel novel)
            LoadNovel(novel);
    }

    private void ChapterList_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ChapterList.SelectedItem is not Chapter chapter)
            return;

        if (!ConfirmUnsavedChanges())
        {
            _loading = true;
            ChapterList.SelectedItem = _currentChapter;
            _loading = false;
            return;
        }

        LoadChapter(chapter);
    }

    private bool ConfirmUnsavedChanges()
    {
        if (_currentChapter == null ||
            ManuscriptEditor.Text == _lastSavedText)
            return true;

        var answer = MessageBox.Show(
            "Save your manuscript changes before continuing?",
            "Unsaved manuscript",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Cancel)
            return false;

        if (answer == MessageBoxResult.Yes)
            SaveCurrentChapter();

        return true;
    }

    private void SaveCurrentChapter()
    {
        if (_currentChapter == null)
            return;

        _currentChapter.Content = ManuscriptEditor.Text;
        _chapters.Update(_currentChapter);

        _lastSavedText = ManuscriptEditor.Text;
        SaveStatusLabel.Text = "Saved";
        AutosaveLabel.Text = $"♧  Saved at {DateTime.Now:HH:mm:ss}";
    }

    private void SaveChapter_Click(object sender, RoutedEventArgs e)
        => SaveCurrentChapter();

    private void ManuscriptEditor_TextChanged(
        object sender, TextChangedEventArgs e)
    {
        UpdateWordCount();

        if (!_loading && _currentChapter != null)
        {
            SaveStatusLabel.Text = "Unsaved";
            AutosaveLabel.Text = "♧  Unsaved changes";
        }
    }

    private void UpdateWordCount()
    {
        if (WordCountLabel == null || ManuscriptEditor == null)
            return;

        int words = ManuscriptEditor.Text.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        WordCountLabel.Text = $"Words: {words:N0}";
    }

    private void NewNovel_Click(object sender, RoutedEventArgs e)
    {
        string? title = Ask("New Novel", "Enter your novel title:");
        if (string.IsNullOrWhiteSpace(title))
            return;

        string genre = Ask("Genre", "Enter the genre:") ?? "Fantasy";

        var novel = _novels.Create(new Novel
        {
            Title = title.Trim(),
            Genre = genre.Trim(),
            Description = ""
        });

        var chapter = _chapters.Create(new Chapter
        {
            NovelId = novel.Id,
            ChapterNumber = 1,
            Title = "The Beginning",
            Content = ""
        });

        LoadNovels(novel.Id);

        ChapterList.SelectedItem = _chapters.GetByNovel(novel.Id)
            .FirstOrDefault(c => c.Id == chapter.Id);
    }

    private void NewChapter_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNovel == null)
        {
            MessageBox.Show("Create or select a novel first.");
            return;
        }

        if (!ConfirmUnsavedChanges())
            return;

        var existing = _chapters.GetByNovel(_currentNovel.Id);
        int number = existing.Count == 0
            ? 1
            : existing.Max(c => c.ChapterNumber) + 1;

        string title = Ask("New Chapter", $"Chapter {number} title:")
            ?? $"Chapter {number}";

        var chapter = _chapters.Create(new Chapter
        {
            NovelId = _currentNovel.Id,
            ChapterNumber = number,
            Title = title
        });

        LoadNovel(_currentNovel);
        ChapterList.SelectedItem = _chapters.GetById(chapter.Id);
    }

    private static string? Ask(string title, string prompt)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 380,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(8, 15, 27)),
            Foreground = Brushes.White,
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock
        {
            Text = prompt,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var input = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 70 };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 70 };

        ok.Click += (_, _) => dialog.DialogResult = true;

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        return dialog.ShowDialog() == true ? input.Text : null;
    }

    private void SelectLocalAI_Click(object sender, RoutedEventArgs e)
    {
        _isOpenAI = false;
        _ai = new OllamaAIService();
        ModelBoxReset();
        AddChatMessage("System",
            "Local AI selected. Ensure Ollama is running and your model is installed.");
    }

    private void SelectOpenAI_Click(object sender, RoutedEventArgs e)
    {
        _isOpenAI = true;
        ModelBoxReset();
        AddChatMessage("System",
            "OpenAI selected. Your API key will be requested when you send a message.");
    }

    private void ModelBoxReset()
    {
        // Provider selection is handled by the provider buttons.
        // Model selection UI can be added to the toolbar in the next pass.
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e)
    {
        string message = ChatInput.Text.Trim();
        if (message.Length == 0)
            return;

        if (!ConfirmUnsavedChanges())
            return;

        ChatInput.Clear();
        AddChatMessage("You", message);
        AddChatMessage("MythForge", "Thinking…");

        try
        {
            if (_isOpenAI)
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    _apiKey = Ask("OpenAI API Key",
                        "Enter your API key (kept in memory for this session):")?.Trim() ?? "";
                }

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    AddChatMessage("System", "OpenAI API key was not entered.");
                    return;
                }

                _ai = new OpenAIService(_apiKey);
            }
            else
            {
                _ai = new OllamaAIService();
            }

            string prompt = BuildStoryPrompt(message);
            string response = await _ai.GenerateResponseAsync(prompt);

            AddChatMessage("MythForge", response);
            AddChatMessage("Draft status",
                "This response is a draft. It has not been inserted into your manuscript.");
        }
        catch (Exception ex)
        {
            AddChatMessage("Error", ex.Message);
        }
    }

    private string BuildStoryPrompt(string message)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine(
            "You are MythForge, a novel-writing assistant. Respect the supplied canon.");
        prompt.AppendLine($"Writing mode: {_mode}");

        if (_currentNovel != null)
        {
            prompt.AppendLine($"Novel: {_currentNovel.Title}");
            prompt.AppendLine($"Genre: {_currentNovel.Genre}");
            prompt.AppendLine($"Description: {_currentNovel.Description}");

            prompt.AppendLine("CHARACTERS:");
            foreach (var character in _characters.GetByNovel(_currentNovel.Id))
            {
                prompt.AppendLine(
                    $"{character.Name}: {character.Role}; {character.Personality}; " +
                    $"Goals: {character.Goals}; Fears: {character.Fears}");
            }

            prompt.AppendLine("APPROVED STORY NOTES:");
            foreach (var note in _notes.GetByNovel(_currentNovel.Id)
                         .Where(n => n.IsCanon))
            {
                prompt.AppendLine(
                    $"[{note.Category}] {note.Title}: {note.Content}");
            }
        }

        if (_currentChapter != null)
        {
            prompt.AppendLine($"Current chapter: {_currentChapter.Title}");
            prompt.AppendLine("Current manuscript context:");
            prompt.AppendLine(ManuscriptEditor.Text);
        }

        prompt.AppendLine("USER REQUEST:");
        prompt.AppendLine(message);
        prompt.AppendLine(
            "Return a separate draft. Never claim that you saved it.");

        return prompt.ToString();
    }

    private void AddChatMessage(string speaker, string message)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(
                speaker == "You"
                    ? Color.FromRgb(20, 34, 53)
                    : Color.FromRgb(15, 25, 39)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(50, 48, 48)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(11),
            Margin = new Thickness(3, 5, 3, 8)
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = speaker,
            Foreground = new SolidColorBrush(Color.FromRgb(213, 173, 112)),
            Margin = new Thickness(0, 0, 0, 5)
        });
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 20
        });

        border.Child = panel;
        ChatMessages.Children.Add(border);
        ChatScrollViewer.ScrollToEnd();
    }

    private void ContinueStory_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Continue the current scene while respecting the saved story context.");

    private void AddDialogue_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Write a dialogue-rich continuation using distinct character voices.");

    private void MakeDarker_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Suggest a darker, more atmospheric continuation of this scene.");

    private void ChangeSetting_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Suggest a setting change that fits the established world.");

    private void GiveMoreDetail_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Expand the sensory detail without contradicting established canon.");

    private void SetQuickPrompt(string prompt)
    {
        ChatInput.Text = prompt;
        ChatInput.Focus();
        ChatInput.CaretIndex = ChatInput.Text.Length;
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) return;

        _redo.Push(ManuscriptEditor.Text);
        _loading = true;
        ManuscriptEditor.Text = _undo.Pop();
        _loading = false;
        UpdateWordCount();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0) return;

        _undo.Push(ManuscriptEditor.Text);
        _loading = true;
        ManuscriptEditor.Text = _redo.Pop();
        _loading = false;
        UpdateWordCount();
    }

    private void SceneMode_Click(object sender, RoutedEventArgs e)
    {
        _mode = _mode == "creative" ? "manual" : "creative";
        MessageBox.Show($"Writing mode: {_mode}");
    }

    private void FontSize_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || FontSizeSelector.SelectedItem is not ComboBoxItem item)
            return;

        if (double.TryParse(item.Content?.ToString(), out double size))
            ManuscriptEditor.FontSize = size;
    }

    private void Bold_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Rich-text formatting will be connected when the editor is upgraded to RichTextBox.");

    private void Italic_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Rich-text formatting will be connected when the editor is upgraded to RichTextBox.");

    private void Underline_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Rich-text formatting will be connected when the editor is upgraded to RichTextBox.");

    private void AlignLeft_Click(object sender, RoutedEventArgs e)
        => ManuscriptEditor.TextAlignment = TextAlignment.Left;

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        int words = ManuscriptEditor.Text.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        MessageBox.Show(
            $"Novel: {_currentNovel?.Title ?? "None"}\n" +
            $"Chapters: {ChapterList.Items.Count}\n" +
            $"Current chapter words: {words}");
    }

    private void NovelOverview_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNovel == null) return;

        _currentNovel.Description =
            Ask("Novel Overview", "Enter the novel description:")
            ?? _currentNovel.Description;

        _novels.Update(_currentNovel);
    }

    private void Chapters_Click(object sender, RoutedEventArgs e)
        => ChapterList.Focus();

    private void Characters_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNovel == null)
        {
            MessageBox.Show("Create or select a novel first.");
            return;
        }

        string? name = Ask("Character Design", "Character name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var character = new Character
        {
            NovelId = _currentNovel.Id,
            Name = name.Trim(),
            Role = Ask("Character Design", "Character role:") ?? "",
            Personality = Ask("Character Design", "Personality traits:") ?? "",
            Goals = Ask("Character Design", "Goals:") ?? "",
            Fears = Ask("Character Design", "Fears:") ?? ""
        };

        _characters.Create(character);
        MessageBox.Show("Character saved.");
    }

    private void WorldBuilder_Click(object sender, RoutedEventArgs e)
        => SaveStoryNote("Worldbuilding");

    private void PlotConnections_Click(object sender, RoutedEventArgs e)
        => SaveStoryNote("Plot");

    private void Timeline_Click(object sender, RoutedEventArgs e)
        => SaveStoryNote("Timeline");

    private void ResearchNotes_Click(object sender, RoutedEventArgs e)
        => SaveStoryNote("Research");

    private void SaveStoryNote(string category)
    {
        if (_currentNovel == null)
        {
            MessageBox.Show("Create or select a novel first.");
            return;
        }

        string? title = Ask(category, "Entry title:");
        if (string.IsNullOrWhiteSpace(title)) return;

        string content = Ask(category, "Entry details:") ?? "";

        bool isCanon = MessageBox.Show(
            "Mark this entry as approved story canon?",
            "Canon confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;

        _notes.Create(new StoryNote
        {
            NovelId = _currentNovel.Id,
            Category = category,
            Title = title.Trim(),
            Content = content,
            IsCanon = isCanon
        });

        MessageBox.Show($"{category} entry saved.");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show(
            "Settings will be expanded in a later stage. " +
            "The current UI reference remains the design target.");

    protected override void OnClosing(
        System.ComponentModel.CancelEventArgs e)
    {
        if (!ConfirmUnsavedChanges())
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }
}
