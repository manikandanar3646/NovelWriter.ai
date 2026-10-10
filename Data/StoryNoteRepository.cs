
using AI_Novel_writing_System.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AI_Novel_writing_System.Data
{
    public class StoryNoteRepository
    {
        private readonly DatabaseService _databaseService;

        public StoryNoteRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService
                ?? throw new ArgumentNullException(nameof(databaseService));
        }

        // Create a new story note.
        public StoryNote Create(StoryNote note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            if (note.NovelId <= 0)
                throw new ArgumentException(
                    "A valid novel ID is required.", nameof(note));

            if (string.IsNullOrWhiteSpace(note.Title))
                throw new ArgumentException(
                    "A note title is required.", nameof(note));

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            note.Title = note.Title.Trim();
            note.Category = note.Category?.Trim() ?? string.Empty;
            note.Content ??= string.Empty;
            note.CreatedAt = note.CreatedAt == default
                ? DateTime.Now
                : note.CreatedAt;
            note.UpdatedAt = DateTime.Now;

            note.Id = collection.Insert(note);

            return note;
        }

        // Retrieve all notes belonging to a novel.
        public List<StoryNote> GetByNovel(int novelId)
        {
            return GetByNovelId(novelId);
        }

        // Alias retained for callers using the longer method name.
        public List<StoryNote> GetByNovelId(int novelId)
        {
            if (novelId <= 0)
                return new List<StoryNote>();

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            return collection
                .Find(note => note.NovelId == novelId)
                .OrderByDescending(note => note.UpdatedAt)
                .ToList();
        }

        // Retrieve only canon notes.
        public List<StoryNote> GetCanonNotes(int novelId)
        {
            if (novelId <= 0)
                return new List<StoryNote>();

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            return collection
                .Find(note =>
                    note.NovelId == novelId && note.IsCanon)
                .OrderByDescending(note => note.UpdatedAt)
                .ToList();
        }

        // Find one note by its ID.
        public StoryNote? GetById(int id)
        {
            if (id <= 0)
                return null;

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            return collection.FindById(id);
        }

        // Search titles, categories, and content within a novel.
        public List<StoryNote> Search(int novelId, string searchText)
        {
            if (novelId <= 0)
                return new List<StoryNote>();

            if (string.IsNullOrWhiteSpace(searchText))
                return GetByNovelId(novelId);

            string query = searchText.Trim();

            return GetByNovelId(novelId)
                .Where(note =>
                    note.Title.Contains(
                        query, StringComparison.OrdinalIgnoreCase)
                    || note.Category.Contains(
                        query, StringComparison.OrdinalIgnoreCase)
                    || note.Content.Contains(
                        query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Update an existing note.
        public bool Update(StoryNote note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            if (note.Id <= 0 || note.NovelId <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(note.Title))
                throw new ArgumentException(
                    "A note title is required.", nameof(note));

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            StoryNote? existing = collection.FindById(note.Id);

            if (existing == null)
                return false;

            // Preserve the original creation timestamp.
            note.CreatedAt = existing.CreatedAt;
            note.Title = note.Title.Trim();
            note.Category = note.Category?.Trim() ?? string.Empty;
            note.Content ??= string.Empty;
            note.UpdatedAt = DateTime.Now;

            return collection.Update(note);
        }

        // Change whether a note is considered canon.
        public bool SetCanon(int noteId, bool isCanon)
        {
            if (noteId <= 0)
                return false;

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            StoryNote? note = collection.FindById(noteId);

            if (note == null)
                return false;

            note.IsCanon = isCanon;
            note.UpdatedAt = DateTime.Now;

            return collection.Update(note);
        }

        // Delete one note.
        public bool Delete(int id)
        {
            if (id <= 0)
                return false;

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            return collection.Delete(id);
        }

        // Delete all notes belonging to a novel.
        // Useful when implementing novel deletion.
        public int DeleteByNovelId(int novelId)
        {
            if (novelId <= 0)
                return 0;

            using var db = _databaseService.GetDatabase();

            var collection = db.GetCollection<StoryNote>("storyNotes");

            return collection.DeleteMany(
                note => note.NovelId == novelId);
        }
    }
}
