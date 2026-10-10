using AI_Novel_writing_System.Data;
using AI_Novel_writing_System.Models;
using darkFanNovel;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    private bool _openAISelected;
    private string _apiKey = "";
    private string _writingMode = "creative";
    private string _savedText = "";

    private readonly System.Collections.Generic.Stack<string> _undo = new();
    private readonly System.Collections.Generic.Stack<string> _redo = new();

    public MainWindow()
    {
        InitializeComponent();

        _novels = new NovelRepository(_database);
        _chapters = new ChapterRepository(_database);
        _characters = new CharacterRepository(_database);
        _notes = new StoryNoteRepository(_database);

        LoadNovels();
        AddChatMessage("MythForge",
            "Your writing companion is ready. Select a novel or create one to begin.");
    }

    private void LoadNovels(int preferredId = 0)
    {
        _loading = true;

        var items = _novels.GetAll();
        NovelSelector.ItemsSource = items;
        NovelSelector.SelectedItem =
            items.FirstOrDefault(n => n.Id == preferredId)
            ?? items.FirstOrDefault();

        _loading = false;

        if (NovelSelector.SelectedItem is Novel novel)
            LoadNovel(novel);
        else
        {
            _currentNovel = null;
            _currentChapter = null;
            ChapterList.ItemsSource = null;
            ManuscriptEditor.Clear();
            CurrentChapterTitle.Text = "Create a novel to begin";
            ChapterSubheading.Text = "No novel selected";
            _savedText = "";
            UpdateWordCount();
        }
    }

    private void LoadNovel(Novel novel)
    {
        _currentNovel = novel;

        _loading = true;
        var chapters = _chapters.GetByNovelId(novel.Id);
        ChapterList.ItemsSource = chapters;
        ChapterList.SelectedItem = chapters.FirstOrDefault();
        ChapterSubheading.Text = $"{chapters.Count} chapters";
        _loading = false;

        if (ChapterList.SelectedItem is Chapter chapter)
            LoadChapter(chapter);
        else
        {
            _currentChapter = null;
            ManuscriptEditor.Clear();
            CurrentChapterTitle.Text = novel.Title;
            _savedText = "";
            UpdateWordCount();
        }
    }

    private void LoadChapter(Chapter chapter)
    {
        _currentChapter = chapter;

        _loading = true;
        ManuscriptEditor.Text = chapter.Content ?? "";
        CurrentChapterTitle.Text =
            $"Chapter {chapter.ChapterNumber} — {chapter.Title}";
        _savedText = ManuscriptEditor.Text;
        _undo.Clear();
        _redo.Clear();
        _loading = false;

        SaveStatusLabel.Text = "Saved";
        AutosaveLabel.Text = "●  Loaded from LiteDB";
        UpdateWordCount();
    }

    private void NovelSelector_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (_loading || NovelSelector.SelectedItem is not Novel novel)
            return;

        if (!ConfirmUnsavedChanges())
        {
            _loading = true;
            NovelSelector.SelectedItem = _currentNovel;
            _loading = false;
            return;
        }

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
            ManuscriptEditor.Text == _savedText)
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
        {
            MessageBox.Show("Create or select a chapter first.");
            return;
        }

        _currentChapter.Content = ManuscriptEditor.Text;
        bool saved = _chapters.Update(_currentChapter);

        if (saved)
        {
            _savedText = ManuscriptEditor.Text;
            SaveStatusLabel.Text = "Saved";
            AutosaveLabel.Text = $"●  Saved at {DateTime.Now:HH:mm:ss}";
        }
        else
        {
            SaveStatusLabel.Text = "Save failed";
            MessageBox.Show("The chapter could not be saved.",
                "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            AutosaveLabel.Text = "●  Unsaved changes";
        }
    }

    private void UpdateWordCount()
    {
        if (WordCountLabel == null || ManuscriptEditor == null)
            return;

        int count = ManuscriptEditor.Text.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        WordCountLabel.Text = $"Words: {count:N0}";
    }

    private void NewNovel_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmUnsavedChanges())
            return;

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

        _chapters.Create(new Chapter
        {
            NovelId = novel.Id,
            ChapterNumber = 1,
            Title = "The Beginning",
            Content = ""
        });

        LoadNovels(novel.Id);
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

        var chapters = _chapters.GetByNovelId(_currentNovel.Id);
        int number = chapters.Count == 0
            ? 1
            : chapters.Max(c => c.ChapterNumber) + 1;

        string title = Ask("New Chapter", $"Title for Chapter {number}:")
            ?? $"Chapter {number}";

        var chapter = _chapters.Create(new Chapter
        {
            NovelId = _currentNovel.Id,
            ChapterNumber = number,
            Title = title.Trim()
        });

        LoadNovel(_currentNovel);

        _loading = true;
        ChapterList.SelectedItem = _chapters.GetById(chapter.Id);
        _loading = false;

        LoadChapter(chapter);
    }

    private static string? Ask(string title, string prompt)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 390,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
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

        var input = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var ok = new Button
        {
            Content = "OK",
            MinWidth = 75,
            IsDefault = true
        };
        var cancel = new Button
        {
            Content = "Cancel",
            MinWidth = 75,
            IsCancel = true
        };

        ok.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        return dialog.ShowDialog() == true ? input.Text : null;
    }

    private void SelectLocalAI_Click(object sender, RoutedEventArgs e)
    {
        _openAISelected = false;
        _ai = new OllamaAIService();

        LocalAIButton.BorderBrush =
            new SolidColorBrush(Color.FromRgb(212, 175, 112));
        OpenAIButton.ClearValue(Control.BorderBrushProperty);

        AddChatMessage("System",
            "Local AI selected. Make sure Ollama is running and the model is installed.");
    }

    private void SelectOpenAI_Click(object sender, RoutedEventArgs e)
    {
        _openAISelected = true;

        OpenAIButton.BorderBrush =
            new SolidColorBrush(Color.FromRgb(212, 175, 112));
        LocalAIButton.ClearValue(Control.BorderBrushProperty);

        AddChatMessage("System",
            "OpenAI selected. You'll be asked for your API key when you send a request.");
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e)
    {
        string message = ChatInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return;

        ChatInput.Clear();
        AddChatMessage("You", message);

        var thinking = AddChatMessage("MythForge", "Thinking…");

        try
        {
            if (_openAISelected)
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    _apiKey = Ask("OpenAI API Key",
                        "Enter your API key. It will be kept in memory for this session.")
                        ?.Trim() ?? "";
                }

                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    ChatMessages.Children.Remove(thinking);
                    AddChatMessage("System", "No API key was entered.");
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

            ChatMessages.Children.Remove(thinking);
            AddChatMessage("MythForge", response);
            AddChatMessage("Draft notice",
                "This is an AI response only. It has not been inserted into your manuscript. Copy it into the editor only when you choose.");
        }
        catch (Exception ex)
        {
            ChatMessages.Children.Remove(thinking);
            AddChatMessage("Error", ex.Message);
        }
    }

    private string BuildStoryPrompt(string message)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(
            "You are MythForge, an AI novel-writing companion. Respect established canon.");
        prompt.AppendLine($"Writing mode: {_writingMode}");

        if (_currentNovel != null)
        {
            prompt.AppendLine($"Novel: {_currentNovel.Title}");
            prompt.AppendLine($"Genre: {_currentNovel.Genre}");
            prompt.AppendLine($"Description: {_currentNovel.Description}");

            prompt.AppendLine("\nCHARACTERS:");
            foreach (var character in _characters.GetByNovelId(_currentNovel.Id))
            {
                prompt.AppendLine(
                    $"{character.Name}; Role: {character.Role}; " +
                    $"Personality: {character.Personality}; " +
                    $"Goals: {character.Goals}; Fears: {character.Fears}");
            }

            prompt.AppendLine("\nAPPROVED CANON NOTES:");
            foreach (var note in _notes.GetByNovel(_currentNovel.Id)
                         .Where(n => n.IsCanon))
            {
                prompt.AppendLine(
                    $"[{note.Category}] {note.Title}: {note.Content}");
            }
        }

        if (_currentChapter != null)
        {
            prompt.AppendLine($"\nCurrent chapter: {_currentChapter.Title}");
            prompt.AppendLine("Current manuscript:");
            prompt.AppendLine(ManuscriptEditor.Text);
        }

        prompt.AppendLine("\nUSER REQUEST:");
        prompt.AppendLine(message);
        prompt.AppendLine(
            "\nReturn the requested text or advice as a draft. Do not claim to save or modify the manuscript.");

        return prompt.ToString();
    }

    private Border AddChatMessage(string speaker, string message)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(
                speaker == "You"
                    ? Color.FromRgb(21, 34, 52)
                    : Color.FromRgb(16, 27, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(48, 59, 73)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(11),
            Margin = new Thickness(3, 5, 3, 7)
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = speaker,
            Foreground = new SolidColorBrush(Color.FromRgb(212, 175, 112)),
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
        return border;
    }

    private void ClearChat_Click(object sender, RoutedEventArgs e)
        => ChatMessages.Children.Clear();

    private void ContinueStory_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Continue the current scene while respecting approved story canon.");

    private void AddDialogue_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Write a dialogue-rich continuation with distinct character voices.");

    private void MakeDarker_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Make the scene darker and more atmospheric without contradicting canon.");

    private void ChangeSetting_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Suggest a setting change that fits the established world.");

    private void GiveMoreDetail_Click(object sender, RoutedEventArgs e)
        => SetQuickPrompt("Expand the sensory details while preserving the events and character motivations.");

    private void SetQuickPrompt(string prompt)
    {
        ChatInput.Text = prompt;
        ChatInput.Focus();
        ChatInput.CaretIndex = ChatInput.Text.Length;
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0)
            return;

        _redo.Push(ManuscriptEditor.Text);
        _loading = true;
        ManuscriptEditor.Text = _undo.Pop();
        _loading = false;
        UpdateWordCount();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0)
            return;

        _undo.Push(ManuscriptEditor.Text);
        _loading = true;
        ManuscriptEditor.Text = _redo.Pop();
        _loading = false;
        UpdateWordCount();
    }

    private void FontSize_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized ||
            FontSizeSelector.SelectedItem is not ComboBoxItem item ||
            !double.TryParse(item.Content?.ToString(), out double size))
            return;

        ManuscriptEditor.FontSize = size;
    }

    private void FontFamily_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized ||
            FontFamilySelector.SelectedItem is not ComboBoxItem item ||
            item.Content is not string family)
            return;

        ManuscriptEditor.FontFamily = new FontFamily(family);
    }

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.FontWeight =
            ManuscriptEditor.FontWeight == FontWeights.Bold
                ? FontWeights.Normal : FontWeights.Bold;
    }

    private void Italic_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.FontStyle =
            ManuscriptEditor.FontStyle == FontStyles.Italic
                ? FontStyles.Normal : FontStyles.Italic;
    }

    private void Underline_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.TextDecorations =
            ManuscriptEditor.TextDecorations == TextDecorations.Underline
                ? null : TextDecorations.Underline;
    }

    private void AlignLeft_Click(object sender, RoutedEventArgs e)
        => ManuscriptEditor.TextAlignment = TextAlignment.Left;

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        int words = ManuscriptEditor.Text.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        MessageBox.Show(
            $"Novel: {_currentNovel?.Title ?? "None"}\n" +
            $"Chapters: {ChapterList.Items.Count}\n" +
            $"Current chapter words: {words}",
            "Dashboard");
    }

    private void NovelOverview_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNovel == null)
        {
            MessageBox.Show("Create or select a novel first.");
            return;
        }

        string? description = Ask(
            "Novel Overview", "Enter the novel description:");

        if (description == null)
            return;

        _currentNovel.Description = description;
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
        if (string.IsNullOrWhiteSpace(name))
            return;

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
        if (string.IsNullOrWhiteSpace(title))
            return;

        string? content = Ask(category, "Entry details:");
        if (content == null)
            return;

        bool canon = MessageBox.Show(
            "Is this an approved fact in your story?",
            "Approve as canon",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;

        _notes.Create(new StoryNote
        {
            NovelId = _currentNovel.Id,
            Category = category,
            Title = title.Trim(),
            Content = content,
            IsCanon = canon
        });

        MessageBox.Show($"{category} entry saved.");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Choose OpenAI or Local AI from the top toolbar. " +
            "OpenAI asks for an API key when you first send a request.",
            "Settings");
    }

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
