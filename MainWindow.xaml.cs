using AI_Novel_writing_System.Data;
using AI_Novel_writing_System.Models;
using AI_Novel_writing_System.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AI_Novel_writing_System;

public partial class MainWindow : Window
{
    private readonly DatabaseService _database = new();

    private readonly NovelRepository _novels;
    private readonly ChapterRepository _chapters;
    private readonly CharacterRepository _characters;
    private readonly StoryNoteRepository _notes;

    private Novel? _currentNovel;
    private Chapter? _currentChapter;

    private IAIService _ai = new OllamaAIService();

    private bool _loading;
    private bool _openAISelected;
    private string _apiKey = string.Empty;
    private string _writingMode = "creative";
    private string _savedText = string.Empty;
    private string _lastEditorText = string.Empty;

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

        AddChatMessage(
            "MythForge",
            "Your writing companion is ready. Select a novel or create one to begin.");
    }

    private void LoadNovels(int preferredId = 0)
    {
        _loading = true;

        try
        {
            List<Novel> novels = _novels.GetAll();

            NovelSelector.ItemsSource = novels;
            NovelSelector.SelectedItem =
                novels.FirstOrDefault(n => n.Id == preferredId)
                ?? novels.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load novels.\n\n{ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _loading = false;
        }

        if (NovelSelector.SelectedItem is Novel novel)
        {
            LoadNovel(novel);
        }
        else
        {
            _currentNovel = null;
            _currentChapter = null;

            ChapterList.ItemsSource = null;

            _loading = true;
            ManuscriptEditor.Clear();
            _loading = false;

            CurrentChapterTitle.Text = "Create a novel to begin";
            ChapterSubheading.Text = "No novel selected";

            _savedText = string.Empty;
            _lastEditorText = string.Empty;

            _undo.Clear();
            _redo.Clear();

            UpdateWordCount();
            UpdateSaveStatus();
        }
    }

    private void LoadNovel(Novel novel)
    {
        _currentNovel = novel;
        _currentChapter = null;

        _loading = true;

        try
        {
            List<Chapter> chapters =
                _chapters.GetByNovelId(novel.Id);

            ChapterList.ItemsSource = chapters;
            ChapterList.SelectedItem = chapters.FirstOrDefault();

            ChapterSubheading.Text = $"{chapters.Count} chapters";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load chapters.\n\n{ex.Message}",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _loading = false;
        }

        if (ChapterList.SelectedItem is Chapter chapter)
        {
            LoadChapter(chapter);
        }
        else
        {
            _currentChapter = null;

            _loading = true;
            ManuscriptEditor.Clear();
            _loading = false;

            CurrentChapterTitle.Text = novel.Title;

            _savedText = string.Empty;
            _lastEditorText = string.Empty;

            _undo.Clear();
            _redo.Clear();

            UpdateWordCount();
            UpdateSaveStatus();
        }
    }

    private void LoadChapter(Chapter chapter)
    {
        _currentChapter = chapter;

        _loading = true;

        try
        {
            ManuscriptEditor.Text = chapter.Content ?? string.Empty;

            CurrentChapterTitle.Text =
                $"Chapter {chapter.ChapterNumber} — {chapter.Title}";

            _savedText = ManuscriptEditor.Text;
            _lastEditorText = ManuscriptEditor.Text;

            _undo.Clear();
            _redo.Clear();

            SaveStatusLabel.Text = "Saved";
            AutosaveLabel.Text = "● Loaded from LiteDB";

            UpdateWordCount();
        }
        finally
        {
            _loading = false;
        }
    }

    private void NovelSelector_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
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
        object sender,
        SelectionChangedEventArgs e)
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
        {
            return true;
        }

        MessageBoxResult answer = MessageBox.Show(
            "Your manuscript has unsaved changes.\n\n" +
            "Would you like to save them before continuing?",
            "Unsaved Manuscript",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Cancel)
            return false;

        if (answer == MessageBoxResult.No)
            return true;

        return SaveCurrentChapter();
    }

    private bool SaveCurrentChapter(bool showMessage = true)
    {
        if (_currentChapter == null)
        {
            if (showMessage)
                MessageBox.Show("Create or select a chapter first.");

            return false;
        }

        try
        {
            _currentChapter.Content = ManuscriptEditor.Text;

            bool saved = _chapters.Update(_currentChapter);

            if (!saved)
            {
                SaveStatusLabel.Text = "Save failed";

                if (showMessage)
                {
                    MessageBox.Show(
                        "The chapter could not be saved. " +
                        "Your editor content has been kept.",
                        "Save Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return false;
            }

            _savedText = ManuscriptEditor.Text;

            SaveStatusLabel.Text = "Saved";
            AutosaveLabel.Text = $"● Saved at {DateTime.Now:HH:mm:ss}";

            return true;
        }
        catch (Exception ex)
        {
            SaveStatusLabel.Text = "Save failed";

            if (showMessage)
            {
                MessageBox.Show(
                    $"An error occurred while saving the chapter.\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return false;
        }
    }

    private void SaveChapter_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentChapter();
    }

    private void ManuscriptEditor_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        UpdateWordCount();

        if (_loading)
            return;

        string currentText = ManuscriptEditor.Text;

        if (currentText != _lastEditorText)
        {
            _undo.Push(_lastEditorText);
            _redo.Clear();

            _lastEditorText = currentText;
        }

        UpdateSaveStatus();
    }

    private void UpdateSaveStatus()
    {
        if (SaveStatusLabel == null || AutosaveLabel == null)
            return;

        if (_currentChapter == null)
        {
            SaveStatusLabel.Text = "No chapter";
            AutosaveLabel.Text = "● No chapter selected";
            return;
        }

        bool hasChanges = ManuscriptEditor.Text != _savedText;

        SaveStatusLabel.Text = hasChanges ? "Unsaved" : "Saved";

        AutosaveLabel.Text = hasChanges
            ? "● Unsaved changes"
            : "● All changes saved";
    }

    private void UpdateWordCount()
    {
        if (WordCountLabel == null || ManuscriptEditor == null)
            return;

        int count = ManuscriptEditor.Text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        WordCountLabel.Text = $"Words: {count:N0}";
    }

    private void NewNovel_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmUnsavedChanges())
            return;

        string? title = Ask("New Novel", "Enter your novel title:");

        if (string.IsNullOrWhiteSpace(title))
            return;

        string? genreInput = Ask("Genre", "Enter the genre:");

        string genre = string.IsNullOrWhiteSpace(genreInput)
            ? "Fantasy"
            : genreInput.Trim();

        try
        {
            Novel novel = _novels.Create(new Novel
            {
                Title = title.Trim(),
                Genre = genre,
                Description = string.Empty
            });

            _chapters.Create(new Chapter
            {
                NovelId = novel.Id,
                ChapterNumber = 1,
                Title = "The Beginning",
                Content = string.Empty
            });

            LoadNovels(novel.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not create the novel.\n\n{ex.Message}",
                "Creation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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

        try
        {
            List<Chapter> chapters =
                _chapters.GetByNovelId(_currentNovel.Id);

            int number = chapters.Count == 0
                ? 1
                : chapters.Max(c => c.ChapterNumber) + 1;

            string? titleInput = Ask(
                "New Chapter",
                $"Title for Chapter {number}:");

            if (titleInput == null)
                return;

            string chapterTitle = string.IsNullOrWhiteSpace(titleInput)
                ? $"Chapter {number}"
                : titleInput.Trim();

            int novelId = _currentNovel.Id;

            Chapter chapter = _chapters.Create(new Chapter
            {
                NovelId = novelId,
                ChapterNumber = number,
                Title = chapterTitle,
                Content = string.Empty
            });

            Novel novel = _currentNovel;

            LoadNovel(novel);

            _loading = true;
            ChapterList.SelectedItem = chapter;
            _loading = false;

            LoadChapter(chapter);
        }
        catch (Exception ex)
        {
            _loading = false;

            MessageBox.Show(
                $"Could not create the chapter.\n\n{ex.Message}",
                "Creation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string? Ask(string title, string prompt)
    {
        Window dialog = new()
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

        StackPanel panel = new()
        {
            Margin = new Thickness(14)
        };

        panel.Children.Add(new TextBlock
        {
            Text = prompt,
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        });

        TextBox input = new()
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        panel.Children.Add(input);

        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        Button ok = new()
        {
            Content = "OK",
            MinWidth = 75,
            IsDefault = true,
            Margin = new Thickness(0, 0, 8, 0)
        };

        Button cancel = new()
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

        return dialog.ShowDialog() == true
            ? input.Text
            : null;
    }

    private void SelectLocalAI_Click(object sender, RoutedEventArgs e)
    {
        _openAISelected = false;
        _ai = new OllamaAIService();

        LocalAIButton.BorderBrush =
            new SolidColorBrush(Color.FromRgb(212, 175, 112));

        OpenAIButton.ClearValue(Control.BorderBrushProperty);

        AddChatMessage(
            "System",
            "Local AI selected. Ensure Ollama is running and your configured model is installed.");
    }

    private void SelectOpenAI_Click(object sender, RoutedEventArgs e)
    {
        _openAISelected = true;

        OpenAIButton.BorderBrush =
            new SolidColorBrush(Color.FromRgb(212, 175, 112));

        LocalAIButton.ClearValue(Control.BorderBrushProperty);

        AddChatMessage(
            "System",
            "OpenAI selected. You will be asked for your API key when you send a request.");
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e)
    {
        string message = ChatInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        ChatInput.Clear();
        AddChatMessage("You", message);

        Border thinking = AddChatMessage("MythForge", "Thinking…");

        try
        {
            if (_openAISelected)
            {
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    _apiKey = Ask(
                        "OpenAI API Key",
                        "Enter your API key. It will be kept in memory for this session.")
                        ?.Trim() ?? string.Empty;
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

            string response =
                await _ai.GenerateResponseAsync(prompt);

            ChatMessages.Children.Remove(thinking);
            AddChatMessage("MythForge", response);

            AddChatMessage(
                "Draft Notice",
                "This is an AI-generated draft. It has not been inserted into your manuscript. Review it and copy it into the editor if you want to keep it.");
        }
        catch (Exception ex)
        {
            ChatMessages.Children.Remove(thinking);

            AddChatMessage(
                "Error",
                $"The AI request failed.\n{ex.Message}");
        }
    }

    private string BuildStoryPrompt(string message)
    {
        StringBuilder prompt = new();

        prompt.AppendLine(
            "You are MythForge, an AI novel-writing companion. " +
            "Respect established canon and avoid contradicting approved story facts.");

        prompt.AppendLine($"Writing mode: {_writingMode}");

        if (_currentNovel != null)
        {
            prompt.AppendLine($"Novel: {_currentNovel.Title}");
            prompt.AppendLine($"Genre: {_currentNovel.Genre}");
            prompt.AppendLine($"Description: {_currentNovel.Description}");

            prompt.AppendLine();
            prompt.AppendLine("CHARACTERS:");

            foreach (Character character in
                     _characters.GetByNovelId(_currentNovel.Id))
            {
                prompt.AppendLine(
                    $"{character.Name}; " +
                    $"Role: {character.Role}; " +
                    $"Personality: {character.Personality}; " +
                    $"Goals: {character.Goals}; " +
                    $"Fears: {character.Fears}; " +
                    $"Background: {character.Background}; " +
                    $"Dialogue style: {character.DialogueStyle}; " +
                    $"Arc: {character.CharacterArc}");
            }

            prompt.AppendLine();
            prompt.AppendLine("APPROVED CANON NOTES:");

            foreach (StoryNote note in
                     _notes.GetByNovel(_currentNovel.Id).Where(n => n.IsCanon))
            {
                prompt.AppendLine(
                    $"[{note.Category}] {note.Title}: {note.Content}");
            }
        }

        if (_currentChapter != null)
        {
            prompt.AppendLine();
            prompt.AppendLine($"Current chapter: {_currentChapter.Title}");
            prompt.AppendLine("CURRENT MANUSCRIPT:");
            prompt.AppendLine(ManuscriptEditor.Text);
        }

        prompt.AppendLine();
        prompt.AppendLine("USER REQUEST:");
        prompt.AppendLine(message);

        prompt.AppendLine();
        prompt.AppendLine(
            "Return the requested text or advice as a draft. " +
            "Do not claim to have saved, edited, or modified the user's manuscript.");

        return prompt.ToString();
    }

    private Border AddChatMessage(string speaker, string message)
    {
        Border border = new()
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

        StackPanel panel = new();

        panel.Children.Add(new TextBlock
        {
            Text = speaker,
            Foreground =
                new SolidColorBrush(Color.FromRgb(212, 175, 112)),
            Margin = new Thickness(0, 0, 0, 5),
            FontWeight = FontWeights.SemiBold
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
    {
        ChatMessages.Children.Clear();
    }

    private void ContinueStory_Click(object sender, RoutedEventArgs e)
    {
        SetQuickPrompt(
            "Continue the current scene while respecting approved story canon.");
    }

    private void AddDialogue_Click(object sender, RoutedEventArgs e)
    {
        SetQuickPrompt(
            "Write a dialogue-rich continuation with distinct character voices.");
    }

    private void MakeDarker_Click(object sender, RoutedEventArgs e)
    {
        SetQuickPrompt(
            "Make the scene darker and more atmospheric without contradicting canon.");
    }

    private void ChangeSetting_Click(object sender, RoutedEventArgs e)
    {
        SetQuickPrompt(
            "Suggest a setting change that fits the established world.");
    }

    private void GiveMoreDetail_Click(object sender, RoutedEventArgs e)
    {
        SetQuickPrompt(
            "Expand the sensory details while preserving events and character motivations.");
    }

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
        _lastEditorText = ManuscriptEditor.Text;
        _loading = false;

        UpdateWordCount();
        UpdateSaveStatus();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (_redo.Count == 0)
            return;

        _undo.Push(ManuscriptEditor.Text);

        _loading = true;
        ManuscriptEditor.Text = _redo.Pop();
        _lastEditorText = ManuscriptEditor.Text;
        _loading = false;

        UpdateWordCount();
        UpdateSaveStatus();
    }
    
    private void FontSize_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsInitialized ||
            FontSizeSelector.SelectedItem is not ComboBoxItem item ||
            !double.TryParse(item.Content?.ToString(), out double size))
        {
            return;
        }

        ManuscriptEditor.FontSize = size;
    }

    private void FontFamily_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsInitialized ||
            FontFamilySelector.SelectedItem is not ComboBoxItem item ||
            item.Content is not string family)
        {
            return;
        }

        ManuscriptEditor.FontFamily = new FontFamily(family);
    }

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.FontWeight =
            ManuscriptEditor.FontWeight == FontWeights.Bold
                ? FontWeights.Normal
                : FontWeights.Bold;
    }

    private void Italic_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.FontStyle =
            ManuscriptEditor.FontStyle == FontStyles.Italic
                ? FontStyles.Normal
                : FontStyles.Italic;
    }

    private void Underline_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.TextDecorations =
            ManuscriptEditor.TextDecorations == TextDecorations.Underline
                ? null
                : TextDecorations.Underline;
    }

    private void AlignLeft_Click(object sender, RoutedEventArgs e)
    {
        ManuscriptEditor.TextAlignment = TextAlignment.Left;
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        int words = ManuscriptEditor.Text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        MessageBox.Show(
            $"Novel: {_currentNovel?.Title ?? "None"}\n" +
            $"Chapters: {ChapterList.Items.Count}\n" +
            $"Current chapter words: {words:N0}",
            "Dashboard",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void NovelOverview_Click(object sender, RoutedEventArgs e)
    {
        if (_currentNovel == null)
        {
            MessageBox.Show("Create or select a novel first.");
            return;
        }

        string? description = Ask(
            "Novel Overview",
            "Enter the novel description:");

        if (description == null)
            return;

        try
        {
            _currentNovel.Description = description;

            if (!_novels.Update(_currentNovel))
            {
                MessageBox.Show(
                    "The novel description could not be saved.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not update the novel description.\n\n{ex.Message}",
                "Update Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Chapters_Click(object sender, RoutedEventArgs e)
    {
        ChapterList.Focus();
    }

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

        Character character = new()
        {
            NovelId = _currentNovel.Id,
            Name = name.Trim(),
            Role = Ask("Character Design", "Character role:") ?? string.Empty,
            Personality = Ask("Character Design", "Personality traits:") ?? string.Empty,
            Goals = Ask("Character Design", "Goals:") ?? string.Empty,
            Fears = Ask("Character Design", "Fears:") ?? string.Empty
        };

        try
        {
            _characters.Create(character);
            MessageBox.Show("Character saved.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save the character.\n\n{ex.Message}",
                "Character Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void WorldBuilder_Click(object sender, RoutedEventArgs e)
    {
        SaveStoryNote("Worldbuilding");
    }

    private void PlotConnections_Click(object sender, RoutedEventArgs e)
    {
        SaveStoryNote("Plot");
    }

    private void Timeline_Click(object sender, RoutedEventArgs e)
    {
        SaveStoryNote("Timeline");
    }

    private void ResearchNotes_Click(object sender, RoutedEventArgs e)
    {
        SaveStoryNote("Research");
    }

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
            "Approve as Canon",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;

        try
        {
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
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save the entry.\n\n{ex.Message}",
                "Story Notes Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Choose OpenAI or Local AI from the top toolbar.\n\n" +
            "OpenAI requests an API key when you first send a request. " +
            "Local AI requires Ollama to be running with the configured model installed.",
            "MythForge Settings",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
